using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Task1
{
    [Transaction(TransactionMode.Manual)]
    public class CreateFloorFromLines : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument UIDoc = commandData.Application.ActiveUIDocument;
            Autodesk.Revit.DB.Document Doc = UIDoc.Document;

            try
            {
                // Get the floor type
                FilteredElementCollector collector = new FilteredElementCollector(Doc);
                FloorType floorType = collector
                    .OfClass(typeof(FloorType))
                    .OfCategory(BuiltInCategory.OST_Floors)
                    .FirstElement() as FloorType;

                // Get the floor type

                Level Level = new FilteredElementCollector(Doc)
                    .OfClass(typeof(Level))
                    .OfCategory(BuiltInCategory.OST_Levels)
                    .FirstElement() as Level;



                List<Line> lines = new List<Line>
            {
               //before arrangement
               
               Line.CreateBound(new XYZ(0, 0, 0), new XYZ(79, 0, 0)),         // Line1
                Line.CreateBound(new XYZ(44, 25, 0), new XYZ(13, 25, 0)),      // Line2
                Line.CreateBound(new XYZ(13, 40, 0), new XYZ(-8, 40, 0)),      // Line3
                Line.CreateBound(new XYZ(55, 34, 0), new XYZ(55, 10, 0)),      // Line4
                Line.CreateBound(new XYZ(79, 34, 0), new XYZ(55, 34, 0)),      // Line5
                Line.CreateBound(new XYZ(0, 20, 0), new XYZ(0, 0, 0)),         // Line6
                Line.CreateBound(new XYZ(55, 10, 0), new XYZ(44, 12, 0)),      // Line7
                Line.CreateBound(new XYZ(-8, 40, 0), new XYZ(-8, 20, 0)),      // Line8
                Line.CreateBound(new XYZ(79, 0, 0), new XYZ(79, 34, 0)),       // Line9
                Line.CreateBound(new XYZ(44, 12, 0), new XYZ(44, 25, 0)),      // Line10
                Line.CreateBound(new XYZ(-8, 20, 0), new XYZ(0, 20, 0)),       // Line11
                Line.CreateBound(new XYZ(13, 25, 0), new XYZ(13, 40, 0))       // Line12
                

                //after arrangement

               //Line.CreateBound(new XYZ(0, 0, 0), new XYZ(79, 0, 0)),         // Line1
                //Line.CreateBound(new XYZ(79, 0, 0), new XYZ(79, 34, 0)),       // Line9
                //Line.CreateBound(new XYZ(79, 34, 0), new XYZ(55, 34, 0)),      // Line5
                //Line.CreateBound(new XYZ(55, 34, 0), new XYZ(55, 10, 0)),      // Line4
                //Line.CreateBound(new XYZ(55, 10, 0), new XYZ(44, 12, 0)),      // Line7
                //Line.CreateBound(new XYZ(44, 12, 0), new XYZ(44, 25, 0)),      // Line10
                //Line.CreateBound(new XYZ(44, 25, 0), new XYZ(13, 25, 0)),      // Line2
                //Line.CreateBound(new XYZ(13, 25, 0), new XYZ(13, 40, 0)),     // Line12
                //Line.CreateBound(new XYZ(13, 40, 0), new XYZ(-8, 40, 0)),      // Line3
                //Line.CreateBound(new XYZ(-8, 40, 0), new XYZ(-8, 20, 0)),      // Line8
                //Line.CreateBound(new XYZ(-8, 20, 0), new XYZ(0, 20, 0)),       // Line11
                //Line.CreateBound(new XYZ(0, 20, 0), new XYZ(0, 0, 0))         // Line6
            };


                CurveArray curveArray = new CurveArray();
                if (IsClosedCurve(lines) == true)
                {
                    curveArray = CreateCurveArrayFromLines(lines);
                }
                else
                {
                    var ReArranged = ReArrange(lines);
                    if (IsClosedCurve(ReArranged) == true)
                        curveArray = CreateCurveArrayFromLines(ReArranged);
                    else
                    {
                        TaskDialog.Show("result", "Cant form a closed loop");
                        return Result.Succeeded;
                    }

                }
                    using (Transaction trans = new Transaction(Doc, "Create Floor"))
                    {
                        trans.Start();
                        Doc.Create.NewFloor(curveArray, floorType, Level, false);

                        trans.Commit();
                    }



                    return Result.Succeeded;
                }
            catch (Exception e)
            {

                TaskDialog.Show("Error", e.Message);
                return Result.Failed;
            }
        }
        public static bool IsClosedCurve(List<Line> Lines)
        {
            if (Lines == null || Lines.Count < 3) { return false; }
            if (Lines[Lines.Count - 1].GetEndPoint(1).ISEqual(Lines[0].GetEndPoint(0)) != true) { return false; }
            for (int i = 0; i < Lines.Count - 1; i++)
            {
                var P1 = Lines[i].GetEndPoint(1);
                var P2 = Lines[i + 1].GetEndPoint(0);
                if (P1.ISEqual(P2) != true) { return false; }

            }

            return true;
        }

        public static List<Line> ReArrange(List<Line> Lines)
        {
            var ReArrangerLines = new List<Line>();
            ReArrangerLines.Add(Lines[0]);
            for (int i = 0; i < Lines.Count-1; i++)
            {
                var L = Lines.Where(l => l.GetEndPoint(0).ISEqual(ReArrangerLines[i].GetEndPoint(1))== true).First();
                ReArrangerLines.Add(L);
            }

            return ReArrangerLines;


        }
        public static CurveArray CreateCurveArrayFromLines(List<Line> Lines)
        {
            CurveArray curveArray = new CurveArray();
            for (int i = 0; i < Lines.Count; i++)
            {
                curveArray.Append(Lines[i]);
            }

            return curveArray;
        }
    }

}
