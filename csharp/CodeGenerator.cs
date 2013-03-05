/* 
 * Example of code generation using Reflection Emit.
 * Target framework: 2.0
 * Author: Kozlov Dmitriy(hummerd@github).
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeGenerator
{
	class Mapper<TIn, TOut>
	{
		protected delegate TOut MapMethod(TIn src);
		protected MapMethod m_mapMethod = null;


		public TOut Map(TIn source)
		{
			if (m_mapMethod == null)
			{
				var mapping = new Dictionary<string, string>() { { "SomeID", "SomeOtherID" } };
				m_mapMethod = GenerateMapMethod(mapping);
			}

			return m_mapMethod(source);
		}


		protected MapMethod GenerateMapMethod(IDictionary<string, string> mapping)
		{
			var dynGeneratorHostAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
				new AssemblyName("Test.Gen, Version=1.0.0.1"),
				AssemblyBuilderAccess.RunAndSave);

			var dynModule = dynGeneratorHostAssembly.DefineDynamicModule(
				"Test.Gen.Mod", 
				"generated.dll");
			var dynType = dynModule.DefineType(
				"Test.MapperOne", 
				TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Public);

			var dynMethod = dynType.DefineMethod(
				"callme",
				MethodAttributes.Static,
				typeof(TOut),
				new Type[] { typeof(TIn) });
			var prm = dynMethod.DefineParameter(1, ParameterAttributes.None, "source");

			GenerateMapMethodBody(dynMethod.GetILGenerator(), prm, mapping);

			var finalType = dynType.CreateType();
			dynGeneratorHostAssembly.Save("generatedasm.dll");

			var realMethodInfo = finalType.GetMethod(dynMethod.Name);

			var methodToken = dynMethod.GetToken().Token;
			var methodInfo = dynModule.ResolveMethod(methodToken);

			return (MapMethod)Delegate.CreateDelegate(
				typeof(MapMethod),
				(MethodInfo)methodInfo);
		}

		protected MapMethod GenerateMapMethod2(IDictionary<string, string> mapping)
		{
			var dynMethod = new DynamicMethod("callme", typeof(TOut), new Type[] { typeof(TIn) });
			var prm = dynMethod.DefineParameter(1, ParameterAttributes.None, "source");

			GenerateMapMethodBody(dynMethod.GetILGenerator(), prm, mapping);

			return (MapMethod)dynMethod.CreateDelegate(typeof(MapMethod));
		}

		protected void GenerateMapMethodBody(ILGenerator gen, ParameterBuilder prmSource, IDictionary<string, string> mapping)
		{
			var tSrc = typeof(TIn);
			var tTarg = typeof(TOut);
			var targetCtor = tTarg.GetConstructor(new Type[0]);

			var locResult = gen.DeclareLocal(tTarg);

			// Генерим result = new TOut();
			gen.Emit(OpCodes.Newobj, targetCtor);
			gen.Emit(OpCodes.Stloc, locResult);

			foreach (var methodMap in mapping)
			{
				var methodSrc = tSrc.GetProperty(methodMap.Key).GetGetMethod();
				var methodTarg = tTarg.GetProperty(methodMap.Value).GetSetMethod();

				// Генерим result.methodTarg = source.methodSrc;
				gen.Emit(OpCodes.Ldloc, locResult);
				gen.Emit(OpCodes.Ldarg, prmSource.Position - 1);
				gen.Emit(OpCodes.Callvirt, methodSrc);
				gen.Emit(OpCodes.Conv_R8);
				gen.Emit(OpCodes.Callvirt, methodTarg);
			}

			// Генерим return result;
			gen.Emit(OpCodes.Ldloc, locResult);
			gen.Emit(OpCodes.Ret);
		}
	}
}
