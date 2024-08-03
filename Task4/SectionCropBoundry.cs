using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task4
{
    [Transaction(TransactionMode.Manual)]
    public class SectionCropBoundry : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument UIDoc = commandData.Application.ActiveUIDocument;
            Autodesk.Revit.DB.Document Doc = UIDoc.Document;
            try
            {    //// Get level

                Level Lev = new FilteredElementCollector(Doc)
                    .OfClass(typeof(Level))
                    .OfCategory(BuiltInCategory.OST_Levels)
                    .Where(l => l.Name == Doc.ActiveView.Name).First() as Level;
                //var Lev =Doc.GetElement( Doc.ActiveView.LevelId) as Level;
                //TaskDialog.Show("flag", Doc.ActiveView.Name);
                var SecRef = UIDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element/*,new SectionViews_SelectionFilter()*/);
                var Sec = Doc.GetElement(SecRef);
                if (Sec != null)
                {
                    var S = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Views)
                        .OfClass(typeof(ViewSection)).WhereElementIsNotElementType()
                        .Cast<View>().Where(v => v.Name == Sec.Name).First();

                    var Max = S.CropBox.Max;
                    var Min = S.CropBox.Min;
                   
                    using (Transaction tr = new Transaction(Doc, "modify sec"))
                    {
                        tr.Start();
                        BoundingBoxXYZ bb = new BoundingBoxXYZ();
                    bb.Min = new XYZ(Min.X, (Lev.Elevation/12) - 10/12, Min.Z);
                    bb.Max = new XYZ(Max.X, (Lev.Elevation/12) + 10/12, Max.Z);
                    bb.Transform = S.CropBox.Transform;
                    S.CropBox = bb;
                        tr.Commit();
                    }


                }


                return Result.Succeeded;
            }
            catch (Exception e)
            {

                TaskDialog.Show("Error", e.Message);
                return Result.Failed;
            }
        }
        //class SectionViews_SelectionFilter : ISelectionFilter
        //{
        //    public bool AllowElement(Element e)
        //    {
        //        //  compare the element's category ID with the built-in category ID
        //        return e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Viewers;
        //    }

        //    public bool AllowReference(Reference r, XYZ p)
        //    {
        //        return true;
        //    }
        //}
    }
}
