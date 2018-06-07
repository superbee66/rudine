using System;
using System.Collections.Generic;
using System.Data.Entity.Design;
using System.Data.Metadata.Edm;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace dCForm.Core.Storage.Sql.Reverser
{
    /// <summary>
    ///     EFCF = Entity Framework Code First
    ///     Reverse engineers SQL database tables (no procs, udfs, etc) & produces strings of cSharp classes compatible with
    ///     Entity Framework's Code First.
    /// </summary>
    internal static class Handler
    {
        private const string USING_STATEMENTS = "using System;\r\nusing System.Collections.Generic;\r\n\r\n";

        internal static string ReverseEngineerCodeFirst(
            string modelsNamespace,
            string schema,
            string connectionString,
            string providerInvariant = "System.Data.SqlClient")
        {
            SqlConnection connection = new SqlConnection(connectionString);

            // Load store schema
            EntityStoreSchemaGenerator _EntityStoreSchemaGenerator = new EntityStoreSchemaGenerator(providerInvariant, connectionString, "dbo")
            {
                GenerateForeignKeyProperties = true
            };
            
            _EntityStoreSchemaGenerator.GenerateStoreMetadata(new[] { new EntityStoreSchemaFilterEntry(null, schema, null, EntityStoreSchemaFilterObjectTypes.Table, EntityStoreSchemaFilterEffect.Allow) });


            // Generate default mapping
            string contextName = connection
                                     .Database
                                     .Replace(" ", string.Empty)
                                     .Replace(".", string.Empty) + "Context";

            EntityModelSchemaGenerator _EntityModelSchemaGenerator =
                new EntityModelSchemaGenerator(
                    _EntityStoreSchemaGenerator.EntityContainer,
                    modelsNamespace,
                    contextName)
                { GenerateForeignKeyProperties = false };

            _EntityModelSchemaGenerator.GenerateMetadata();

            // Pull out info about types to be generated
            EntityType[] _EntityTypes = _EntityModelSchemaGenerator.EdmItemCollection.OfType<EntityType>().ToArray();
            EdmMapping _EdmMapping = new EdmMapping(_EntityModelSchemaGenerator, _EntityStoreSchemaGenerator.StoreItemCollection);

            string result = string.Empty;
            foreach (EntityType type in _EntityTypes)
                result = result + new Entity
                {
                    Host = new TextTemplatingEngineHost
                    {
                        EntityType = type,
                        EntityContainer = _EntityModelSchemaGenerator.EntityContainer,
                        Namespace = modelsNamespace,
                        TableSet = _EdmMapping.EntityMappings[type].Item1,
                        PropertyToColumnMappings = _EdmMapping.EntityMappings[type].Item2,
                        ManyToManyMappings = _EdmMapping.ManyToManyMappings
                    }
                }
                                      .TransformText()
                                      // remove existing using statements as all csharp class code blocks will concat to form one string of code
                                      .Replace(USING_STATEMENTS, string.Empty);

            return string.IsNullOrWhiteSpace(result)
                ? string.Empty
                : result.Insert(0, USING_STATEMENTS); //add using statements back to the top of the csharp document
        }

        private class EdmMapping
        {
            public EdmMapping(EntityModelSchemaGenerator mcGenerator, ItemCollection store)
            {
                // Pull mapping xml out
                XmlDocument mappingDoc = new XmlDocument();
                StringBuilder mappingXml = new StringBuilder();

                using (StringWriter textWriter = new StringWriter(mappingXml))
                    mcGenerator.WriteStorageMapping(new XmlTextWriter(textWriter));

                mappingDoc.LoadXml(mappingXml.ToString());

                IEnumerable<EntitySet> entitySets = mcGenerator.EntityContainer.BaseEntitySets.OfType<EntitySet>();
                IEnumerable<AssociationSet> associationSets = mcGenerator.EntityContainer.BaseEntitySets.OfType<AssociationSet>();
                IEnumerable<EntitySet> tableSets = store.OfType<EntityContainer>().Single().BaseEntitySets.OfType<EntitySet>();

                EntityMappings = BuildEntityMappings(mappingDoc, entitySets, tableSets);
                ManyToManyMappings = BuildManyToManyMappings(mappingDoc, associationSets, tableSets);
            }

            public Dictionary<EntityType, Tuple<EntitySet, Dictionary<EdmProperty, EdmProperty>>> EntityMappings { get; }

            public Dictionary<AssociationType, Tuple<EntitySet, Dictionary<RelationshipEndMember, Dictionary<EdmMember, string>>>> ManyToManyMappings { get; }

            private static Dictionary<EntityType, Tuple<EntitySet, Dictionary<EdmProperty, EdmProperty>>> BuildEntityMappings(XmlDocument mappingDoc, IEnumerable<EntitySet> entitySets, IEnumerable<EntitySet> tableSets)
            {
                // Build mapping for each type
                Dictionary<EntityType, Tuple<EntitySet, Dictionary<EdmProperty, EdmProperty>>> mappings = new Dictionary<EntityType, Tuple<EntitySet, Dictionary<EdmProperty, EdmProperty>>>();
                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(mappingDoc.NameTable);
                namespaceManager.AddNamespace("ef", mappingDoc.ChildNodes[0].NamespaceURI);
                foreach (EntitySet entitySet in entitySets)
                {
                    // Post VS2010 builds use a different structure for mapping
                    XmlNode setMapping = mappingDoc.ChildNodes[0].NamespaceURI == "http://schemas.microsoft.com/ado/2009/11/mapping/cs"
                                             ? mappingDoc.SelectSingleNode(string.Format("//ef:EntitySetMapping[@Name=\"{0}\"]/ef:EntityTypeMapping/ef:MappingFragment", entitySet.Name), namespaceManager)
                                             : mappingDoc.SelectSingleNode(string.Format("//ef:EntitySetMapping[@Name=\"{0}\"]", entitySet.Name), namespaceManager);

                    string tableName = setMapping.Attributes["StoreEntitySet"].Value;
                    EntitySet tableSet = tableSets.Single(s => s.Name == tableName);

                    Dictionary<EdmProperty, EdmProperty> propertyMappings = new Dictionary<EdmProperty, EdmProperty>();
                    foreach (EdmProperty prop in entitySet.ElementType.Properties)
                    {
                        XmlNode propMapping = setMapping.SelectSingleNode(string.Format("./ef:ScalarProperty[@Name=\"{0}\"]", prop.Name), namespaceManager);
                        string columnName = propMapping.Attributes["ColumnName"].Value;
                        EdmProperty columnProp = tableSet.ElementType.Properties[columnName];

                        propertyMappings.Add(prop, columnProp);
                    }

                    mappings.Add(entitySet.ElementType, Tuple.Create(tableSet, propertyMappings));
                }

                return mappings;
            }

            private static Dictionary<AssociationType, Tuple<EntitySet, Dictionary<RelationshipEndMember, Dictionary<EdmMember, string>>>> BuildManyToManyMappings(XmlDocument mappingDoc, IEnumerable<AssociationSet> associationSets, IEnumerable<EntitySet> tableSets)
            {
                // Build mapping for each association
                Dictionary<AssociationType, Tuple<EntitySet, Dictionary<RelationshipEndMember, Dictionary<EdmMember, string>>>> mappings = new Dictionary<AssociationType, Tuple<EntitySet, Dictionary<RelationshipEndMember, Dictionary<EdmMember, string>>>>();
                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(mappingDoc.NameTable);
                namespaceManager.AddNamespace("ef", mappingDoc.ChildNodes[0].NamespaceURI);
                foreach (AssociationSet associationSet in associationSets.Where(a => !a.ElementType.AssociationEndMembers.Where(e => e.RelationshipMultiplicity != RelationshipMultiplicity.Many).Any()))
                {
                    XmlNode setMapping = mappingDoc.SelectSingleNode(string.Format("//ef:AssociationSetMapping[@Name=\"{0}\"]", associationSet.Name), namespaceManager);
                    string tableName = setMapping.Attributes["StoreEntitySet"].Value;
                    EntitySet tableSet = tableSets.Single(s => s.Name == tableName);

                    Dictionary<RelationshipEndMember, Dictionary<EdmMember, string>> endMappings = new Dictionary<RelationshipEndMember, Dictionary<EdmMember, string>>();
                    foreach (AssociationSetEnd end in associationSet.AssociationSetEnds)
                    {
                        Dictionary<EdmMember, string> propertyToColumnMappings = new Dictionary<EdmMember, string>();
                        XmlNode endMapping = setMapping.SelectSingleNode(string.Format("./ef:EndProperty[@Name=\"{0}\"]", end.Name), namespaceManager);
                        foreach (XmlNode fk in endMapping.ChildNodes)
                        {
                            string propertyName = fk.Attributes["Name"].Value;
                            EdmProperty property = end.EntitySet.ElementType.Properties[propertyName];
                            string columnName = fk.Attributes["ColumnName"].Value;
                            propertyToColumnMappings.Add(property, columnName);
                        }

                        endMappings.Add(end.CorrespondingAssociationEndMember, propertyToColumnMappings);
                    }

                    mappings.Add(associationSet.ElementType, Tuple.Create(tableSet, endMappings));
                }

                return mappings;
            }
        }
    }
}