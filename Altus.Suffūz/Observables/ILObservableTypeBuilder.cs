﻿using Altus.Suffūz.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Altus.Suffūz.Observables
{
    public class ILObservableTypeBuilder
    {
        private static AssemblyName _asmName = new AssemblyName() { Name = "Altus.Suffūz.Observables" };
        private static ModuleBuilder _modBuilder;
        private static AssemblyBuilder _asmBuilder;
        private static Dictionary<Type, Type> _types = new Dictionary<Type, Type>();

        static ILObservableTypeBuilder()
        {
            _asmBuilder = Thread.GetDomain().DefineDynamicAssembly(_asmName, AssemblyBuilderAccess.RunAndSave);
            _modBuilder = _asmBuilder.DefineDynamicModule(_asmName.Name + ".dll", true);
        }

        public Type Build(Type type)
        {
            Type subType;
            lock(_types)
            {
                if (!_types.TryGetValue(type, out subType))
                {
                    subType = Create(type);
                    _types.Add(type, subType);
                }
            }
            return subType;
        }

        public void SaveAssembly()
        {
            _asmBuilder.Save(_asmName + ".dll");
        }

        private Type Create(Type type)
        {
            var interfaceType = typeof(IObservable<>).MakeGenericType(type);
            var className = GetTypeName(type);
            var typeBuilder = _modBuilder.DefineType(
                className,
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable | TypeAttributes.Sealed,
                type, // base type
                new Type[] { interfaceType } // interfaces
                );

            /*

            IObservable<T> --------------
            string GlobalKey { get; }
            T Instance { get; }
            ExclusiveLock SyncLock { get; }

            */


            ImplementCtor(typeBuilder, 
                ImplementProperty<ExclusiveLock>(typeBuilder, "SyncLock"), 
                ImplementProperty(typeBuilder, "Instance", type), 
                ImplementProperty<string>(typeBuilder, "GlobalKey"),
                ImplementProperty<IPublisher>(typeBuilder, "Publisher"));

            ImplementPropertyProxies(typeBuilder);
            ImplementMethodProxies(typeBuilder);

            return typeBuilder.CreateType();
        }

        private void ImplementMethodProxies(TypeBuilder typeBuilder)
        {
            
        }

        private void ImplementPropertyProxies(TypeBuilder typeBuilder)
        {
            var commutativeProperties = GetVirtualProperties<CommutativeEventAttribute>(typeBuilder.BaseType);
            foreach(var property in commutativeProperties)
            {
                ImplementCommutativeProperty(typeBuilder, property);
            }
        }

        private void ImplementCommutativeProperty(TypeBuilder typeBuilder, PropertyInfo property)
        {
            /*
            .property instance int32 Size()
            {
              .get instance int32 'Altus.Suffūz.Observables.Tests.Observables'.Observable_StateClass::get_Size()
              .set instance void 'Altus.Suffūz.Observables.Tests.Observables'.Observable_StateClass::set_Size(int32)
            } // end of property Observable_StateClass::Size

            .method public hidebysig specialname virtual 
            instance int32  get_Size() cil managed
            {
              // Code size       12 (0xc)
              .maxstack  1
              .locals init ([0] int32 V_0)
              IL_0000:  nop
              IL_0001:  ldarg.0
              IL_0002:  call       instance int32 'Altus.Suffūz.Observables.Tests.Observables'.StateClass::get_Size()
              IL_0007:  stloc.0
              IL_0008:  br.s       IL_000a
              IL_000a:  ldloc.0
              IL_000b:  ret
            } // end of method Observable_StateClass::get_Size

            .method public hidebysig specialname virtual 
            instance void  set_Size(int32 'value') cil managed
            {
              // Code size       143 (0x8f)
              .maxstack  9
              .locals init ([0] class ['Altus.Suffūz']'Altus.Suffūz.Observables'.PropertyUpdate`2<class 'Altus.Suffūz.Observables.Tests.Observables'.StateClass,int32> beforeChange,
                       [1] class ['Altus.Suffūz']'Altus.Suffūz.Observables'.PropertyUpdate`2<class 'Altus.Suffūz.Observables.Tests.Observables'.StateClass,int32> afterChange)
              IL_0000:  nop
              .try
              {
                IL_0001:  nop
                IL_0002:  ldarg.0
                IL_0003:  call       instance class ['Altus.Suffūz']'Altus.Suffūz.Threading'.ExclusiveLock 'Altus.Suffūz.Observables.Tests.Observables'.Observable_StateClass::get_SyncLock()
                IL_0008:  callvirt   instance void ['Altus.Suffūz']'Altus.Suffūz.Threading'.ExclusiveLock::Enter()
                IL_000d:  nop
                IL_000e:  ldarg.0
                IL_000f:  call       instance string 'Altus.Suffūz.Observables.Tests.Observables'.Observable_StateClass::get_GlobalKey()
                IL_0014:  ldc.i4.0
                IL_0015:  ldstr      "Size"
                IL_001a:  ldtoken    'Altus.Suffūz.Observables.Tests.Observables'.StateClass
                IL_001f:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
                IL_0024:  ldarg.0
                IL_0025:  ldc.i4.1   // Commutative
                IL_0026:  ldc.i4.2   // Additive
                IL_0027:  ldarg.0
                IL_0028:  call       instance int32 'Altus.Suffūz.Observables.Tests.Observables'.StateClass::get_Size()
                IL_002d:  ldarg.1
                IL_002e:  newobj     instance void class ['Altus.Suffūz']'Altus.Suffūz.Observables'.PropertyUpdate`2<class 'Altus.Suffūz.Observables.Tests.Observables'.StateClass,int32>::.ctor(string,
                                                                                                                                                                                                    valuetype ['Altus.Suffūz']'Altus.Suffūz.Observables'.OperationState,
                                                                                                                                                                                                    string,
                                                                                                                                                                                                    class [mscorlib]System.Type,
                                                                                                                                                                                                    !0,
                                                                                                                                                                                                    valuetype ['Altus.Suffūz']'Altus.Suffūz.Observables'.EventClass,
                                                                                                                                                                                                    valuetype ['Altus.Suffūz']'Altus.Suffūz.Observables'.EventOrder,
                                                                                                                                                                                                    !1,
                                                                                                                                                                                                    !1)
                IL_0033:  stloc.0
                IL_0034:  ldarg.0
                IL_0035:  call       instance class ['Altus.Suffūz']'Altus.Suffūz.Observables'.IPublisher 'Altus.Suffūz.Observables.Tests.Observables'.Observable_StateClass::get_Publisher()
                IL_003a:  ldloc.0
                IL_003b:  callvirt   instance void ['Altus.Suffūz']'Altus.Suffūz.Observables'.IPublisher::Publish<class 'Altus.Suffūz.Observables.Tests.Observables'.StateClass,int32>(class ['Altus.Suffūz']'Altus.Suffūz.Observables'.PropertyUpdate`2<!!0,!!1>)
                IL_0040:  nop
                IL_0041:  ldarg.0
                IL_0042:  ldarg.1
                IL_0043:  call       instance void 'Altus.Suffūz.Observables.Tests.Observables'.StateClass::set_Size(int32)
                IL_0048:  nop
                IL_0049:  ldarg.0
                IL_004a:  call       instance string 'Altus.Suffūz.Observables.Tests.Observables'.Observable_StateClass::get_GlobalKey()
                IL_004f:  ldc.i4.1
                IL_0050:  ldstr      "Size"
                IL_0055:  ldtoken    'Altus.Suffūz.Observables.Tests.Observables'.StateClass
                IL_005a:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
                IL_005f:  ldarg.0
                IL_0060:  ldc.i4.1
                IL_0061:  ldc.i4.2
                IL_0062:  ldarg.0
                IL_0063:  call       instance int32 'Altus.Suffūz.Observables.Tests.Observables'.StateClass::get_Size()
                IL_0068:  ldarg.1
                IL_0069:  newobj     instance void class ['Altus.Suffūz']'Altus.Suffūz.Observables'.PropertyUpdate`2<class 'Altus.Suffūz.Observables.Tests.Observables'.StateClass,int32>::.ctor(string,
                                                                                                                                                                                                    valuetype ['Altus.Suffūz']'Altus.Suffūz.Observables'.OperationState,
                                                                                                                                                                                                    string,
                                                                                                                                                                                                    class [mscorlib]System.Type,
                                                                                                                                                                                                    !0,
                                                                                                                                                                                                    valuetype ['Altus.Suffūz']'Altus.Suffūz.Observables'.EventClass,
                                                                                                                                                                                                    valuetype ['Altus.Suffūz']'Altus.Suffūz.Observables'.EventOrder,
                                                                                                                                                                                                    !1,
                                                                                                                                                                                                    !1)
                IL_006e:  stloc.1
                IL_006f:  ldarg.0
                IL_0070:  call       instance class ['Altus.Suffūz']'Altus.Suffūz.Observables'.IPublisher 'Altus.Suffūz.Observables.Tests.Observables'.Observable_StateClass::get_Publisher()
                IL_0075:  ldloc.1
                IL_0076:  callvirt   instance void ['Altus.Suffūz']'Altus.Suffūz.Observables'.IPublisher::Publish<class 'Altus.Suffūz.Observables.Tests.Observables'.StateClass,int32>(class ['Altus.Suffūz']'Altus.Suffūz.Observables'.PropertyUpdate`2<!!0,!!1>)
                IL_007b:  nop
                IL_007c:  nop
                IL_007d:  leave.s    IL_008e
              }  // end .try
              finally
              {
                IL_007f:  nop
                IL_0080:  ldarg.0
                IL_0081:  call       instance class ['Altus.Suffūz']'Altus.Suffūz.Threading'.ExclusiveLock 'Altus.Suffūz.Observables.Tests.Observables'.Observable_StateClass::get_SyncLock()
                IL_0086:  callvirt   instance void ['Altus.Suffūz']'Altus.Suffūz.Threading'.ExclusiveLock::Exit()
                IL_008b:  nop
                IL_008c:  nop
                IL_008d:  endfinally
              }  // end handler
              IL_008e:  ret
            } // end of method Observable_StateClass::set_Size
            */

            var overridenProperty = typeBuilder.DefineProperty(property.Name,
                PropertyAttributes.HasDefault,
                property.PropertyType,
                null);

            var getter = typeBuilder.DefineMethod(property.GetMethod.Name,
                property.GetMethod.Attributes,
                property.PropertyType,
                Type.EmptyTypes);

            var getterCode = getter.GetILGenerator();
            getterCode.Emit(OpCodes.Ldarg_0);
            getterCode.Emit(OpCodes.Call, property.GetMethod);
            getterCode.Emit(OpCodes.Ret);
            overridenProperty.SetGetMethod(getter);
            typeBuilder.DefineMethodOverride(getter, property.GetMethod);

            var setter = typeBuilder.DefineMethod(property.SetMethod.Name,
                property.SetMethod.Attributes,
                null,
                new[] { property.PropertyType });

            var setterCode = setter.GetILGenerator();
            setterCode.Emit(OpCodes.Ldarg_0);
            setterCode.Emit(OpCodes.Ldarg_1);
            setterCode.Emit(OpCodes.Call, property.SetMethod);
            setterCode.Emit(OpCodes.Ret);
            overridenProperty.SetSetMethod(setter);
            typeBuilder.DefineMethodOverride(setter, property.SetMethod);
        }

        private IEnumerable<MethodInfo> GetVirtualMethods<T>(Type type) where T : Attribute
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                       .Where(mi => mi.GetCustomAttribute<T>() != null && mi.IsVirtual);
        }

        private IEnumerable<PropertyInfo> GetVirtualProperties<T>(Type type) where T : Attribute
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                       .Where(mi => ((mi is PropertyInfo) && ((PropertyInfo)mi).CanRead && ((PropertyInfo)mi).CanWrite)
                                    && mi.GetCustomAttribute<T>() != null
                                    && mi.GetMethod.IsVirtual
                                    && mi.SetMethod.IsVirtual);
        }

        private ConstructorInfo ImplementCtor(TypeBuilder typeBuilder, 
            PropertyInfo exclusiveLockProp, 
            PropertyInfo instanceProp, 
            PropertyInfo globalKeyProp,
            PropertyInfo publisherProp)
        {
            /*
            .method public hidebysig specialname rtspecialname 
            instance void  .ctor(class ['Altus.Suffūz']'Altus.Suffūz.Observables'.IPublisher publisher,
                                 class 'Altus.Suffūz.Observables.Tests.Observables'.StateClass 'instance',
                                 string globalKey) cil managed
            {
              // Code size       46 (0x2e)
              .maxstack  8
              IL_0000:  ldarg.0
              IL_0001:  call       instance void 'Altus.Suffūz.Observables.Tests.Observables'.StateClass::.ctor()
              IL_0006:  nop
              IL_0007:  nop
              IL_0008:  ldarg.0
              IL_0009:  ldarg.3
              IL_000a:  newobj     instance void ['Altus.Suffūz']'Altus.Suffūz.Threading'.ExclusiveLock::.ctor(string)
              IL_000f:  call       instance void 'Altus.Suffūz.Observables.Tests.Observables'.Observable_StateClass::set_SyncLock(class ['Altus.Suffūz']'Altus.Suffūz.Threading'.ExclusiveLock)
              IL_0014:  nop
              IL_0015:  ldarg.0
              IL_0016:  ldarg.3
              IL_0017:  call       instance void 'Altus.Suffūz.Observables.Tests.Observables'.Observable_StateClass::set_GlobalKey(string)
              IL_001c:  nop
              IL_001d:  ldarg.0
              IL_001e:  ldarg.2
              IL_001f:  call       instance void 'Altus.Suffūz.Observables.Tests.Observables'.Observable_StateClass::set_Instance(class 'Altus.Suffūz.Observables.Tests.Observables'.StateClass)
              IL_0024:  nop
              IL_0025:  ldarg.0
              IL_0026:  ldarg.1
              IL_0027:  call       instance void 'Altus.Suffūz.Observables.Tests.Observables'.Observable_StateClass::set_Publisher(class ['Altus.Suffūz']'Altus.Suffūz.Observables'.IPublisher)
              IL_002c:  nop
              IL_002d:  ret
            } // end of method Observable_StateClass::.ctor
            */

            var ctorBuilder = typeBuilder.DefineConstructor(
               MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
               CallingConventions.Standard,
               new Type[] { typeof(IPublisher), typeBuilder.BaseType, typeof(string) });
            var baseType = typeBuilder.BaseType;
            var ctorCode = ctorBuilder.GetILGenerator();

            ctorCode.Emit(OpCodes.Ldarg_0);
            ctorCode.Emit(OpCodes.Call, baseType.GetConstructor(new Type[0]));
            ctorCode.Emit(OpCodes.Ldarg_0);
            ctorCode.Emit(OpCodes.Ldarg_3); // global key arg
            ctorCode.Emit(OpCodes.Newobj, typeof(ExclusiveLock).GetConstructors().First()); // there's only one
            ctorCode.Emit(OpCodes.Call, exclusiveLockProp.GetSetMethod(true)); // create a new lock, using the global key
            ctorCode.Emit(OpCodes.Ldarg_0);
            ctorCode.Emit(OpCodes.Ldarg_3); // global key arg
            ctorCode.Emit(OpCodes.Call, globalKeyProp.GetSetMethod(true)); // set the global key property
            ctorCode.Emit(OpCodes.Ldarg_0);
            ctorCode.Emit(OpCodes.Ldarg_2); // instance arg
            ctorCode.Emit(OpCodes.Call, instanceProp.GetSetMethod(true)); // set instance property
            ctorCode.Emit(OpCodes.Ldarg_0);
            ctorCode.Emit(OpCodes.Ldarg_1); // publisher arg
            ctorCode.Emit(OpCodes.Call, publisherProp.GetSetMethod(true)); // set publisher
            ctorCode.Emit(OpCodes.Ret); // return self
            return ctorBuilder;
        }

        private PropertyInfo ImplementProperty<T>(TypeBuilder typeBuilder, string propertyName, MethodAttributes setterAttributes = MethodAttributes.Private)
        {
            return ImplementProperty(typeBuilder, propertyName, typeof(T), setterAttributes);
        }

        private PropertyInfo ImplementProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType, MethodAttributes setterAttributes = MethodAttributes.Private)
        {
            var propType = propertyType;
            var piName = propertyName;
            var backingField = typeBuilder.DefineField("_" + piName.ToLower(), propType, FieldAttributes.Public);

            var property = typeBuilder.DefineProperty(piName,
                PropertyAttributes.HasDefault,
                propType,
                null);

            var getter = typeBuilder.DefineMethod("get_" + piName,
                MethodAttributes.Public
                | MethodAttributes.SpecialName
                | MethodAttributes.HideBySig
                | MethodAttributes.NewSlot
                | MethodAttributes.Final
                | MethodAttributes.Virtual,
                propType,
                Type.EmptyTypes);

            var getterCode = getter.GetILGenerator();
            getterCode.Emit(OpCodes.Ldarg_0);
            getterCode.Emit(OpCodes.Ldfld, backingField);
            getterCode.Emit(OpCodes.Ret);
            property.SetGetMethod(getter);

            var setter = typeBuilder.DefineMethod("set_" + piName,
                setterAttributes
                | MethodAttributes.SpecialName
                | MethodAttributes.HideBySig
                | MethodAttributes.NewSlot
                | MethodAttributes.Final
                | MethodAttributes.Virtual,
                null,
                new[] { propType });

            var setterCode = setter.GetILGenerator();
            setterCode.Emit(OpCodes.Ldarg_0);
            setterCode.Emit(OpCodes.Ldarg_1);
            setterCode.Emit(OpCodes.Stfld, backingField);
            setterCode.Emit(OpCodes.Ret);
            property.SetSetMethod(setter);

            return property;
        }


        private string GetTypeName(Type type)
        {
            string name = type.Namespace + ".suff_";
            GetTypeName(ref name, type);
            return name;
        }

        private void GetTypeName(ref string name, Type type)
        {
            if (type.IsGenericType)
            {
                var genType = type.GetGenericTypeDefinition().Name.Replace("<", "").Replace(">", "").Replace(",", "").Replace("`", "");
                name += "_" + genType;

                foreach (var t in type.GetGenericArguments())
                {
                    GetTypeName(ref name, t);
                }
            }
            else
            {
                name += "_" + type.Name;
            }
        }
    }
}
