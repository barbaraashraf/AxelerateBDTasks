using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Task1;
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
               // TaskDialog.Show("flag", Lev.Elevation.ToString());
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
                        //BoundingBoxXYZ bb = new BoundingBoxXYZ();
                        //bb.Min = new XYZ(Min.X, (Lev.Elevation / 2) + 5, Min.Z);
                        //bb.Max = new XYZ(Max.X, (Lev.Elevation / 2) + 5, Max.Z);
                        //bb.Transform = Transform.CreateTranslation(XYZ.Zero);
                        //S.get_BoundingBox(S).Max = bb.Max;
                        ViewCropRegionShapeManager vcrShapeMgr = S.GetCropRegionShapeManager();
                        var crpShape = vcrShapeMgr.GetCropShape().First();
                        var curves = crpShape.ToList();
                        CurveLoop newcrv = new CurveLoop();
                        var LL = new List<Line>();
                        for (int i = 0; i < curves.Count; i++)
                        {
                            var x = curves[i].GetEndPoint(0).X;
                            var xx = curves[i].GetEndPoint(1).X;

                            var y = curves[i].GetEndPoint(0).Y;
                            var yy = curves[i].GetEndPoint(1).Y;

                            //var z = curves[i].GetEndPoint(0).Z;
                            //var zz = curves[i].GetEndPoint(1).Z;
                            if (i == 0 )
                            {
                              var end = new XYZ(x, y, Lev.Elevation + 10) + (-1*( curves[i]as Line).Direction).Multiply(20);
                                var L = Line.CreateBound(new XYZ(x, y,Lev.Elevation+10 ), end);
                                LL.Add(L);
                                
                            }
                            else if (i == 1)
                            {
                                var L = Line.CreateBound(LL[0].GetEndPoint(1), new XYZ(xx, yy, Lev.Elevation - 10));
                                LL.Add(L);
                            }
                            else if (i == 2)
                            {
                                var end = LL[1].GetEndPoint(1) + (-1 * (curves[i] as Line).Direction).Multiply(20);
                                var L = Line.CreateBound(LL[1].GetEndPoint(1), end);
                                LL.Add(L);
                            }
                            else if (i == 3)
                            {
                                var L = Line.CreateBound(LL[2].GetEndPoint(1), new XYZ(xx, yy, Lev.Elevation + 10));
                                LL.Add(L);
                               // newcrv.Append(L);
                            }
                        }

                      // LL=  CreateFloorFromLines.ReArrange(LL);
                        for (int i = 0; i < LL.Count; i++)
                        {
                            newcrv.Append(LL[i]);

                        }
                        vcrShapeMgr.SetCropShape(newcrv);
                        //var P1 = new XYZ(Min.X, Lev.Elevation - 10, Min.Z);
                        //var P2 = new XYZ(Max.X, Lev.Elevation - 10, Min.Z);
                        //var P3 = new XYZ(Max.X, Lev.Elevation + 10, Max.Z);
                        //var P4 = new XYZ(Min.X, Lev.Elevation + 10, Max.Z);
                        //var L1 = Line.CreateBound(P1, P2);
                        //var L2 = Line.CreateBound(P2, P3);
                        //var L3 = Line.CreateBound(P3, P4);
                        //var L4 = Line.CreateBound(P4, P1);

                        //CurveLoop Clp= new CurveLoop();
                        //Clp.Append(L1);
                        //Clp.Append(L2);
                        //Clp.Append(L3);
                        //Clp.Append(L4);
                        //vcrShapeMgr.SetCropShape(Clp);
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
