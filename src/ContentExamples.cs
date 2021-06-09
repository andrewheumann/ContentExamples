using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;

namespace ContentExamples
{
    public static class ContentExamples
    {
        /// <summary>
        /// The ContentExamples function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A ContentExamplesOutputs instance containing computed results and the model with any new elements.</returns>
        public static ContentExamplesOutputs Execute(Dictionary<string, Model> inputModels, ContentExamplesInputs input)
        {
            var output = new ContentExamplesOutputs();

            // set up material
            var defaultMaterial = new Material("Default", new Color(0.7, 0.7, 0.7, 1.0));

            // A content element is just a pointer to a GLB file, stored at a public URL. 
            var chairGlbLocation = "https://hypar-content-catalogs.s3-us-west-2.amazonaws.com/ade0eff6-151c-473a-b41f-94dbbf1267ad/Steelcase+-+Seating+-+Think+465+Series+-+Value+Package+Work+Chair+-+Upholstery+Back_Adjustable+Arm_Caster.glb";
            var simpleContentElement = new ContentElement(chairGlbLocation,
              new BBox3(new Vector3(0, 0, 0), new Vector3(1, 1, 1)),
              1,
              new Vector3(),
              new Transform(),
              defaultMaterial,
              null,
              true, // It's important to set "IsElementDefinition" to true, so you can make instances.
              Guid.NewGuid(),
              "Chair",
              "{}");

            // Most of these additional properties on the constructor are utilized on revit import, 
            // but generally you don't have to worry about them.

            // I often write a helper method so I don't have to write all this over and over. 
            // See `MakeContentElement` below — this lets you write the line above like this:
            // var simpleContentElement = MakeContentElement(chairGlbLocation, defaultMaterial, chair);

            // To use a content element, make an instance of it, and add it to the model like any other element.
            var chairInstance = simpleContentElement.CreateInstance(new Transform(2, 2, 0), "Chair Instance");
            output.Model.AddElement(chairInstance);

            // Let's make a few more elements. I'm going to store them in a dictionary, so they're easy to use and place later:

            var contentDict = new Dictionary<string, ContentElement> {
              {"Chair",
                MakeContentElement(
                  "https://hypar-content-catalogs.s3-us-west-2.amazonaws.com/ade0eff6-151c-473a-b41f-94dbbf1267ad/Steelcase+-+Seating+-+Think+465+Series+-+Value+Package+Work+Chair+-+Upholstery+Back_Adjustable+Arm_Caster.glb",
                  defaultMaterial, "Chair")
              },
              {
                "Desk Surface",
                 MakeContentElement(
                  "https://hypar-content-catalogs.s3-us-west-2.amazonaws.com/ade0eff6-151c-473a-b41f-94dbbf1267ad/Steelcase+-+Universal+Storage+-+Square+Edge+Top+-+Flush+Front+-+Laminate+-+Common+Top+-+15D+x+60W.glb",
                  defaultMaterial, "Desk Surface")
              },
              {
                "Desk Legs",
                MakeContentElement("https://hypar-content-catalogs.s3-us-west-2.amazonaws.com/ade0eff6-151c-473a-b41f-94dbbf1267ad/Steelcase+-+Migration+SE+-+Desk+-+Rectangular+-+C+Leg+-+29D+x+58W.glb",
                defaultMaterial, "Desk Legs")
              }
            };

            // let's make an instance of each of these. We'll set up points to locate the desk relative to the chair. 
            // It happens that the desk surface and the legs have the same origin, so we can share a transform
            // between them.

            var charLocation = new Transform();
            // the desk needs to be rotated 180°, and then moved over. 
            var deskOffset = new Transform(Vector3.Origin, 180).Concatenated(new Transform(0.73, -0.75, 0));
            var deskLocated = contentDict["Chair"].CreateInstance(charLocation, "Chair");
            var deskSurfaceLocated = contentDict["Desk Surface"].CreateInstance(deskOffset, "Desk Surface");
            var deskLegsLocated = contentDict["Desk Legs"].CreateInstance(deskOffset, "Desk Legs");

            // And then let's add those instances to the model
            output.Model.AddElements(deskLocated, deskSurfaceLocated, deskLegsLocated);

            // It can be useful to hold on to collections of element instances that represent a whole furniture group, 
            // so we can easily make copies of that group. 

            var deskGroup = new List<ElementInstance> { deskLocated, deskSurfaceLocated, deskLegsLocated };

            for (int i = 0; i < 10; i++)
            {
                var groupLocation = new Vector3(i * 2, 6, 0);
                var groupTransform = new Transform(groupLocation);
                foreach (var instance in deskGroup)
                {
                    // We grab the transform we assigned to the instance initially.
                    // This represents its position relative to other elements in the group.
                    var transformInGroup = instance.Transform;
                    // then we concatenate (add) the transform for where we want the group to be
                    var newTransform = transformInGroup.Concatenated(groupTransform);
                    // and then create a new instance, utilizing the same base definition
                    var newInstance = instance.BaseDefinition.CreateInstance(newTransform, $"Desk Group {i}");
                    // and add it to the model.
                    output.Model.AddElement(newInstance);
                }
            }

            // To use a catalog exported from Revit, you can use the 
            //        hypar generate-catalog -u <CATALOG_URL>
            // command to codegen a static class that creates ContentElement objects
            // from a web-hosted catalog. At this time only Hypar employees can create
            // new catalogs.

            // Here are the URLs to some furniture catalogs you might want: 

            // Open Office:     https://hypar-content-catalogs.s3-us-west-2.amazonaws.com/ade0eff6-151c-473a-b41f-94dbbf1267ad/openoffice-ade0eff6-151c-473a-b41f-94dbbf1267ad.json
            // Conference Room: https://hypar-content-catalogs.s3-us-west-2.amazonaws.com/2290ea5e-98aa-429d-8fab-1f260458bf57/ConfRoom-2290ea5e-98aa-429d-8fab-1f260458bf57.json
            // Lounge / Social: https://hypar-content-catalogs.s3-us-west-2.amazonaws.com/5e796702-15a4-47bb-bbfa-1dfa3f6db835/Lounge-5e796702-15a4-47bb-bbfa-1dfa3f6db835.json
            // Private Office:  https://hypar-content-catalogs.s3-us-west-2.amazonaws.com/f3f745d0-1331-437b-a9f5-577d3b816213/PrivateOffice-f3f745d0-1331-437b-a9f5-577d3b816213.json
            // Classroom:       https://hypar-content-catalogs.s3-us-west-2.amazonaws.com/da0d0a40-aff1-4bda-a118-8e432fa8b08c/Classroom-da0d0a40-aff1-4bda-a118-8e432fa8b08c.json
            // Pantry:          https://hypar-content-catalogs.s3-us-west-2.amazonaws.com/8d181754-144e-489f-b41f-e8af01d2b9ec/Pantry-8d181754-144e-489f-b41f-e8af01d2b9ec.json
            // Phone Booth:     https://hypar-content-catalogs.s3-us-west-2.amazonaws.com/a2f43d35-f698-40f4-95b2-a748a2232637/PhoneBooths-a2f43d35-f698-40f4-95b2-a748a2232637.json
            // Reception:       https://hypar-content-catalogs.s3-us-west-2.amazonaws.com/9c5774d0-afdf-417d-964d-f0aea92eba8b/Reception-9c5774d0-afdf-417d-964d-f0aea92eba8b.json

            // Within this project, I've already run the `generate-catalog` command on the "Reception" catalog.
            // I recommend moving this file into the /src directory. 
            // This command created a class called Reception in Reception.g.cs.
            // Its static members, like "ReceptionDesk18950ReceptionDesk18950", are just ContentElements we can make instances of.
            // I can use it like this:
            var receptionDesk = Reception.ReceptionDesk18950ReceptionDesk18950.CreateInstance(new Transform(-4, -6, 0), "Reception Desk");
            output.Model.AddElement(receptionDesk);

            // We can also access the Reception.All collection, which is a list of all the content elements in the catalog.
            // Let's loop over this list and place each item, so we can see what's in the catalog:
            double xPosition = 0.0;
            foreach (var item in Reception.All)
            {
                var location = new Transform(xPosition, -10, 0);
                var bbox = item.BoundingBox;
                // It can be useful to access the ContentElement's bounding box, set when the catalog is
                // created, so you know how big an item is. The origin of an item may not be
                // at its corner, so we can use this information to get a predictable location for it.
                var translationToCorner = new Transform(-item.BoundingBox.Min.X, -item.BoundingBox.Min.Y, 0);
                location.Concatenate(translationToCorner);

                var inst = item.CreateInstance(location, item.Name);
                output.Model.AddElement(inst);
                xPosition += bbox.Max.X - bbox.Min.X + 1.0; // place each item with 1m of spacing between
                // visualize the bounding box
                output.Model.AddElements(bbox.ToModelCurves(location));
            }

            return output;
        }

        /// <summary>
        /// This is a helper method for creating content elements from just a URL.
        /// </summary>
        /// <param name="url">The URL of the content GLB to place</param>
        /// <param name="mat">The material to give this instance</param>
        /// <param name="name">The name to assign to the instance</param>
        /// <returns></returns>
        public static ContentElement MakeContentElement(string url, Material mat, string name)
        {
            return new ContentElement(url,
                new BBox3(new Vector3(0, 0, 0), new Vector3(1, 1, 1)),
                1,
                new Vector3(),
                new Transform(),
                mat,
                null,
                true,
                Guid.NewGuid(),
                name);
        }
    }
}