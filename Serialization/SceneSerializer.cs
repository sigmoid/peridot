
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Peridot;
using Peridot.EntityComponentScene.Serialization;

public class SceneSerializer
{
    public static string SerializeScene(Scene scene)
    {
        var doc = new XDocument();
        var root = new XElement("Scene");
        doc.Add(root);

        var entitiesElement = new XElement("Entities");
        root.Add(entitiesElement);

        foreach (var entity in scene.GetEntities())
        {
            entitiesElement.Add(entity.Serialize());
        }

        return doc.ToString();
    }

    public static Scene DeserializeScene(string xml)
    {
        var doc = XDocument.Parse(xml);
        var scene = new Scene();

        var entitiesElement = doc.Root?.Element("Entities");
        if (entitiesElement != null)
        {
            foreach (var entityElement in entitiesElement.Elements("Entity"))
            {
                var entity = EntityFactory.FromXElement(entityElement);
                scene.AddEntity(entity);
            }
        }

        return scene;
    }
}