using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cs2TsExample.DoItYourself.TypeScriptInterfaces
{
    public static class TypeScriptInterfacesExtension
    {
        private static Type[] nonPrimitivesExcludeList = new Type[]
        {
            typeof(object),
            typeof(string),
            typeof(decimal),
            typeof(void),
        };

        private static IDictionary<Type, string> convertedTypes = new Dictionary<Type, string>()
        {
            [typeof(string)]    = "string",
            [typeof(char)]      = "string",
            [typeof(byte)]      = "number",
            [typeof(sbyte)]     = "number",
            [typeof(short)]     = "number",
            [typeof(ushort)]    = "number",
            [typeof(int)]       = "number",
            [typeof(uint)]      = "number",
            [typeof(long)]      = "number",
            [typeof(ulong)]     = "number",
            [typeof(float)]     = "number",
            [typeof(double)]    = "number",
            [typeof(decimal)]   = "number",
            [typeof(bool)]      = "boolean",
            [typeof(object)]    = "any",
            [typeof(void)]      = "void",
        };


        public static void GenerateTypeScriptInterfaces(this IApplicationBuilder app, string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Type[] typesToConvert = GetTypesToConvert(Assembly.GetExecutingAssembly());

            foreach (Type type in typesToConvert)
            {
                var tsType = ConvertCs2Ts(type);
                string fullPath = Path.Combine(path, tsType.Name);

                string directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllLines(fullPath, tsType.Lines);
            }
        }

        private static Type[] GetTypesToConvert(Assembly assembly)
        {
            Type controllerBaseType = typeof(Microsoft.AspNetCore.Mvc.ControllerBase);

            ISet<Type> actionAttributeTypes = new HashSet<Type>()
            {
                typeof(Microsoft.AspNetCore.Mvc.HttpGetAttribute),
                typeof(Microsoft.AspNetCore.Mvc.HttpPostAttribute),
            };

            var controllers = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(controllerBaseType));

            var actions = controllers.SelectMany(c => c.GetMethods()
                .Where(m => m.IsPublic && m.GetCustomAttributes().Any(a => actionAttributeTypes.Contains(a.GetType())))
            );

            var types = actions.SelectMany(m => new Type[1] { m.ReturnType }.Concat(m.GetParameters().Select(p => p.ParameterType)));

            return types
                .Select(t => ReplaceByGenericArgument(t))
                .Where(t => !t.IsPrimitive && !nonPrimitivesExcludeList.Contains(t))
                .Distinct()
                .ToArray();
        }

        private static Type ReplaceByGenericArgument(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (!type.IsConstructedGenericType)
            {
                return type;
            }

            var genericArgument = type.GenericTypeArguments.First();

            var isTask = type.GetGenericTypeDefinition() == typeof(Task<>);
            var isActionResult = type.GetGenericTypeDefinition() == typeof(Microsoft.AspNetCore.Mvc.ActionResult<>);
            var isEnumerable = typeof(IEnumerable<>).MakeGenericType(genericArgument).IsAssignableFrom(type);

            if (!isTask && !isActionResult && !isEnumerable)
            {
                // Decidir o que fazer com actions que retornam tipos genéricos
                throw new InvalidOperationException();
            }

            if (genericArgument.IsConstructedGenericType)
            {
                return ReplaceByGenericArgument(genericArgument);
            }

            return genericArgument;
        }

        private static (string Name, string[] Lines) ConvertCs2Ts(Type type)
        {
            string filename = $"{type.Namespace.Replace(".", "/")}/{type.Name}.d.ts";

            Type[] types = GetAllNestedTypes(type);

            var lines = new List<string>();

            foreach (Type t in types)
            {
                lines.Add($"");

                if (t.IsClass || t.IsInterface)
                {
                    ConvertClassOrInterface(lines, t);
                }
                else if (t.IsEnum) {
                    ConvertEnum(lines, t);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            return (filename, lines.ToArray());
        }
        
        private static void ConvertClassOrInterface(IList<string> lines, Type type)
        {
            lines.Add($"export interface {type.Name} {{");

            foreach (PropertyInfo property in type.GetProperties().Where(p => p.GetMethod.IsPublic))
            {
                Type propType = property.PropertyType;
                Type arrayType = GetArrayOrEnumerableType(propType);
                Type nullableType = GetNullableType(propType);

                Type typeToUse = nullableType ?? arrayType ?? propType;


                var convertedType = ConvertType(typeToUse);

                string suffix = "";
                suffix = arrayType != null ? "[]" : suffix;
                suffix = nullableType != null ? "|null" : suffix;

                lines.Add($"  {CamelCaseName(property.Name)}: {convertedType}{suffix};");
            }

            lines.Add($"}}");
        }

        private static string ConvertType(Type typeToUse)
        {
            if (convertedTypes.ContainsKey(typeToUse))
            {
                return convertedTypes[typeToUse];
            }

            if (typeToUse.IsConstructedGenericType && typeToUse.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                var keyType = typeToUse.GenericTypeArguments[0];
                var valueType = typeToUse.GenericTypeArguments[1];
                return $"{{ [key: {ConvertType(keyType)}]: {ConvertType(valueType)} }}";
            }

            return typeToUse.Name;
        }

        private static void ConvertEnum(IList<string> lines, Type type)
        {
            var enumValues = type.GetEnumValues().Cast<int>().ToArray();
            var enumNames = type.GetEnumNames();

            lines.Add($"export enum {type.Name} {{");

            for (int i = 0; i < enumValues.Length; i++)
            {
                lines.Add($"  {enumNames[i]} = {enumValues[i]},");
            }

            lines.Add($"}}");
        }

        private static Type[] GetAllNestedTypes(Type type)
        {
            return new Type[] { type }
                .Concat(type.GetNestedTypes().SelectMany(nt => GetAllNestedTypes(nt)))
                .ToArray();
        }

        private static Type GetArrayOrEnumerableType(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            else if (type.IsConstructedGenericType)
            {
                Type typeArgument = type.GenericTypeArguments.First();

                if (typeof(IEnumerable<>).MakeGenericType(typeArgument).IsAssignableFrom(type))
                {
                    return typeArgument;
                }
            }

            return null;
        }

        private static Type GetNullableType(Type type)
        {
            if (type.IsConstructedGenericType)
            {
                Type typeArgument = type.GenericTypeArguments.First();

                if (typeArgument.IsValueType && typeof(Nullable<>).MakeGenericType(typeArgument).IsAssignableFrom(type))
                {
                    return typeArgument;
                }
            }

            return null;
        }

        private static string CamelCaseName(string pascalCaseName)
        {
            return pascalCaseName[0].ToString().ToLower() + pascalCaseName.Substring(1);
        }
    }
}
