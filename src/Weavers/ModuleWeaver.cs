using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using Mono.Cecil;

namespace Weavers
{
    public class ModuleWeaver
    {
        public ModuleWeaver()
        {
            LogInfo = Console.WriteLine;
            LogError = Console.WriteLine;
        }

        public ModuleDefinition ModuleDefinition { get; set; }
        public Action<string> LogInfo { get; set; }
        public Action<string> LogError { get; set; }
        public IAssemblyResolver AssemblyResolver { get; set; }
        public static MethodReference DataMemberTypeCtor { get; set; }
        public static MethodReference DataContractTypeCtor { get; set; }
        public static MethodReference DisplayTypeCtor { get; set; }
        public static TypeReference DisplayTypeAttribute { get; set; }
        public string SolutionDirectoryPath { get; set; }

        public void Execute()
        {
            FindReferences(AssemblyResolver, ModuleDefinition);
            foreach (TypeDefinition type in ModuleDefinition.Types)
            {
                LogInfo(type.FullName);
                AddDataContractAttribute(type);
                int i = 0;
                foreach (PropertyDefinition prop in type.Properties)
                {
                    i++;
                    AddSimpleDebuggerDisplayAttribute(ModuleDefinition, type, prop, i);
                }
            }
        }

        private static void AddSimpleDebuggerDisplayAttribute(ModuleDefinition moduleDefinition, TypeDefinition type,
            PropertyDefinition prop, int i)
        {
            AddOrderAttribute(type, prop, i);
            AddDisplayAttribute(type, prop, i);
        }

        private static void AddDataContractAttribute(TypeDefinition type)
        {
            var attr = new CustomAttribute(DataContractTypeCtor);
            type.CustomAttributes.Add(attr);
        }


        private static void AddOrderAttribute(TypeDefinition type, PropertyDefinition prop, int i)
        {
            var attr = new CustomAttribute(DataMemberTypeCtor);
            TypeReference namedPropertyTypeRef = type.Module.Import(typeof (int));
            attr.Properties.Add(new CustomAttributeNamedArgument("Order",
                new CustomAttributeArgument(namedPropertyTypeRef, i)));
            prop.CustomAttributes.Add(attr);
        }

        private static void AddDisplayAttribute(TypeDefinition type, PropertyDefinition prop, int i)
        {
            // Don't add the property if it already exists
            if (prop.CustomAttributes.Any(c => c.AttributeType.Name == DisplayTypeAttribute.Name))
            {
                return;
            }

            var attr = new CustomAttribute(DisplayTypeCtor);
            string propertyName = string.Empty;
            foreach (char c in prop.Name)
            {
                if (Char.IsUpper(c))
                {
                    propertyName += " " + c;
                }
                else
                {
                    propertyName += c;
                }
            }
            attr.Properties.Add(new CustomAttributeNamedArgument("Name",
                new CustomAttributeArgument(type.Module.Import(typeof (string)), propertyName.TrimStart())));
            attr.Properties.Add(new CustomAttributeNamedArgument("Order",
                new CustomAttributeArgument(type.Module.Import(typeof (int)), i)));

            prop.CustomAttributes.Add(attr);
        }


        public static void FindReferences(IAssemblyResolver assemblyResolver, ModuleDefinition moduleDefinition)
        {
            DataContractTypeCtor = moduleDefinition.Import(
                typeof(DataContractAttribute).GetConstructor(new Type[0]));
            DataMemberTypeCtor = moduleDefinition.Import(
                typeof (DataMemberAttribute).GetConstructor(new Type[0]));
            DisplayTypeAttribute = moduleDefinition.Import(typeof (DisplayAttribute));
            DisplayTypeCtor = moduleDefinition.Import(typeof (DisplayAttribute).GetConstructor(new Type[0]));
        }
    }
}