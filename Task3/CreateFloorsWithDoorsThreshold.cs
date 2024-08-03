using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task1;

namespace Task3
{
    [Transaction(TransactionMode.Manual)]
    public class CreateFloorsWithDoorsThreshold : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument UIDoc = commandData.Application.ActiveUIDocument;
            Autodesk.Revit.DB.Document Doc = UIDoc.Document;

            try
            {
                #region FLoor and level if needed 
                //// Get the floor type
                //FilteredElementCollector collector = new FilteredElementCollector(Doc);
                //FloorType floorType = collector
                //    .OfClass(typeof(FloorType))
                //    .OfCategory(BuiltInCategory.OST_Floors)
                //    .FirstElement() as FloorType;

                //// Get level

                //Level Level = new FilteredElementCollector(Doc)
                //    .OfClass(typeof(Level))
                //    .OfCategory(BuiltInCategory.OST_Levels)
                //    .FirstElement() as Level; 
                #endregion

                // Get doors

                var Doors = new FilteredElementCollector(Doc)
                    .OfCategory(BuiltInCategory.OST_Doors).OfClass(typeof(FamilyInstance))
                    .WhereElementIsNotElementType().Cast<FamilyInstance>()
                    .ToList();
                // Get rooms
                var Rooms = new FilteredElementCollector(Doc)
                   .OfCategory(BuiltInCategory.OST_Rooms)
                   .WhereElementIsNotElementType()
                   .ToList();


                // a class created to use
                var FloorsData = new List<FloorData>();

                // Create a new instance of SpatialElementBoundaryOptions 
                SpatialElementBoundaryOptions op = new SpatialElementBoundaryOptions();
                using (Transaction trans = new Transaction(Doc, "Create floors from rooms"))
                {
                    trans.Start();
                    // 1- get rooms with their boundaries
                    for (int i = 0; i < Rooms.Count; i++)
                    {
                        var R = Rooms[i] as Room;
                        //SpatialElementBoundarySubface boundarySubface = (Rooms[i] as Room).get;
                        var Segments = (Rooms[i] as SpatialElement).GetBoundarySegments(op).First().ToList();
                        var FData = new FloorData();
                        FData.Room = R;
                        FData.RoomBoundary = Segments;
                        FData.TotalBoundry = Segments.Select(s => s.GetCurve()).ToList();
                        // 2- get doors needed for each room
                        FData.Doors = Doors.Where(d => (d.ToRoom?.Id.IntegerValue == R.Id.IntegerValue) ||
                                                        (d.FromRoom?.Id.IntegerValue == R.Id.IntegerValue)).ToList();
                        if (FData.Doors.Count != 0)
                        {
                            FloorsData.Add(FData);
                        }
                        else
                        {
                            FData.Create_Floor(Doc);
                        }

                    }



                    // 3- get closed loops from doors
                    // 3-1 get location point
                    // 3-2 get segment with same door hand directon
                    // 3-3 project updated location points to it original boundry
                    // 3-4 and the new loop to original boundry

                    // loop on rooms with doors only
                    for (int i = 0; i < FloorsData.Count; i++)
                    {
                        var FD = FloorsData[i];
                        var Drs = FD.Doors;
                        var DoorsLines = new List<Line>();
                        //lines from doors to be connected to room boundry
                        DoorsCurve(Drs, DoorsLines);
                        var R = FD.Room;

                        for (int j = 0; j < DoorsLines.Count; j++)
                        {   // 3-4 and the new loop to original boundry
                            AddDoorLineToRoomBoundry(FD, DoorsLines[j]);

                        }
                        // create floor 
                        FD.Create_Floor(Doc);




                    }
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
        #region MyRegion
        //public static CurveArray CreateCurveArrayFromSegments(List<BoundarySegment> segments)
        //{
        //    CurveArray curveArray = new CurveArray();
        //    for (int i = 0; i < segments.Count; i++)
        //    {
        //        curveArray.Append(segments[i].GetCurve());
        //    }

        //    return curveArray;
        //} 
        #endregion

        public static void DoorsCurve(List<FamilyInstance> Drs, List<Line> DoorsLines)
        {

            for (int i = 0; i < Drs.Count; i++)
            {
                var D = Drs[i];

                var Width = D.Symbol.LookupParameter("Width").AsDouble();

                var LP = (D.Location as LocationPoint).Point;
                XYZ endPoint = LP + D.HandOrientation.Multiply(Width / 2);
                var startPoint = LP + D.HandOrientation.Multiply(-1 * (Width / 2));

                DoorsLines.Add(Line.CreateBound(endPoint, startPoint));

            }

        }

        public static void AddDoorLineToRoomBoundry(FloorData FD, Line DoorLine)
        {
            var TB = FD.TotalBoundry; // Total boundary of the floor
            var R = FD.Room; // Room data
            var RoomPoint = (R.Location as LocationPoint).Point; // Location point of the room
            Line FlagLine; // Line used as a reference for intersection
            var LinesToReplaceTheSegment = new List<Line>(); // List of new lines to replace the segment
            Curve seg; // Segment of the boundary to be replaced
            Line segLine; // Casted segment line

            // Check if DoorLine is vertical
            if (Math.Abs(DoorLine.Direction.Y) == 1)
            {
                // Create a flag line based on the DoorLine's end point and room's X-coordinate
                var X = RoomPoint.X;
                FlagLine = Line.CreateBound(DoorLine.GetEndPoint(1),
                                            new XYZ(X, DoorLine.GetEndPoint(1).Y, DoorLine.GetEndPoint(1).Z));

                // Find the segment that intersects with the FlagLine
                seg = TB.Where(s => GetIntersection(s as Line, FlagLine) != null).ToList().First();
                segLine = seg as Line;

                // Gather points from DoorLine and the intersected segment
                var Points = new List<XYZ>
        {
            DoorLine.GetEndPoint(0),
            DoorLine.GetEndPoint(1),
            segLine.GetEndPoint(0),
            segLine.GetEndPoint(1)
        };

                // Order points by Y-coordinate and adjust if necessary
                Points = Points.OrderBy(p => p.Y).ToList();
                if (segLine.GetEndPoint(0).Y != Points[0].Y)
                {
                    Points.Reverse();
                }

                // Project points to the segment line
                var second = segLine.Project(Points[1]).XYZPoint;
                var fifth = segLine.Project(Points[2]).XYZPoint;
                Points.Insert(1, second);
                Points.Insert(4, fifth);

                // Create new lines to replace the segment
                for (int i = 0; i < Points.Count - 1; i++)
                {
                    var L = Line.CreateBound(Points[i], Points[i + 1]);
                    LinesToReplaceTheSegment.Add(L);
                }
            }
            else
            {
                // DoorLine is horizontal, create a flag line based on the DoorLine's end point and room's Y-coordinate
                var Y = RoomPoint.Y;
                FlagLine = Line.CreateBound(DoorLine.GetEndPoint(1),
                                            new XYZ(DoorLine.GetEndPoint(1).X, Y, DoorLine.GetEndPoint(1).Z));

                // Find the segment that intersects with the FlagLine
                seg = TB.Where(s => GetIntersection(s as Line, FlagLine) != null).ToList().First();
                segLine = seg as Line;

                // Gather points from DoorLine and the intersected segment
                var Points = new List<XYZ>
        {
            DoorLine.GetEndPoint(0),
            DoorLine.GetEndPoint(1),
            segLine.GetEndPoint(0),
            segLine.GetEndPoint(1)
        };

                // Order points by X-coordinate and adjust if necessary
                Points = Points.OrderBy(p => p.X).ToList();
                if (segLine.GetEndPoint(0).X != Points[0].X)
                {
                    Points.Reverse();
                }

                // Project points to the segment line
                var second = segLine.Project(Points[1]).XYZPoint;
                var fifth = segLine.Project(Points[2]).XYZPoint;
                Points.Insert(1, second);
                Points.Insert(4, fifth);

                // Create new lines to replace the segment
                for (int i = 0; i < Points.Count - 1; i++)
                {
                    var L = Line.CreateBound(Points[i], Points[i + 1]);
                    LinesToReplaceTheSegment.Add(L);
                }
            }

            // Replace the old segment in the total boundary with the new lines
            for (int i = 0; i < TB.Count; i++)
            {
                if (TB[i].Id == segLine.Id)
                {
                    // Remove the old segment
                    FD.TotalBoundry.Remove(segLine);

                    // Insert the new lines at the same position
                    for (int j = 0; j < LinesToReplaceTheSegment.Count; j++)
                    {
                        FD.TotalBoundry.Insert(i + j, LinesToReplaceTheSegment[j] as Curve);
                    }
                    break; // Break after replacing the segment to avoid unnecessary iterations
                }
            }
        }

        public static XYZ GetIntersection(Line line1, Line line2)
        {
            IntersectionResultArray results;
            SetComparisonResult result = line1.Intersect(line2, out results);

            if (result != SetComparisonResult.Overlap || results == null || results.Size != 1)
            {
                return null; // No intersection or multiple intersections
            }

            return results.get_Item(0).XYZPoint;
        }



    }
}
