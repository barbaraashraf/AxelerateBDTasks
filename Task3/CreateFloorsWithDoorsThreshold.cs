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
            { // Get the floor type
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

                // Get the doors

                var Doors = new FilteredElementCollector(Doc)
                    .OfCategory(BuiltInCategory.OST_Doors).OfClass(typeof(FamilyInstance))
                    .WhereElementIsNotElementType().Cast<FamilyInstance>()
                    .ToList();

                var Rooms = new FilteredElementCollector(Doc)
                   .OfCategory(BuiltInCategory.OST_Rooms)
                   .WhereElementIsNotElementType()
                   .ToList();

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
                        // 2- get doors needed for each room
                        FData.Doors = Doors.Where(d => (d.ToRoom?.Id.IntegerValue == R.Id.IntegerValue) ||
                                                        (d.FromRoom?.Id.IntegerValue == R.Id.IntegerValue)).ToList();
                        if (FData.Doors.Count != 0)
                        {
                            FloorsData.Add(FData);
                        }
                        else
                        { Doc.Create.NewFloor(CreateCurveArrayFromSegments(FData.RoomBoundary), floorType, Level, false); }

                    }
                    // 3- get closed loops from doors
                    //  3-1 get location point // 3-2 get segment with same host diraction // 3-3 project updated location points to it 


                    for (int i = 0; i < FloorsData.Count; i++)
                    { var FD = FloorsData[i];
                        var Drs = FD.Doors;
                        var DoorsLines = new List<Line>();
                        DoorsCurve(Drs, DoorsLines);
                        var R = FD.Room;

                        for (int j = 0; j < DoorsLines.Count; j++)
                        {

                          
                            //var Loop1 = CreateDoorsLoops(R, DoorsLines[j]).Select(e=> e as Curve) as IList<Curve> ;
                            //var x = CurveLoop.Create(Loop1);
                              IList<CurveLoop> Cloop = new List<CurveLoop>();
                            //Cloop.Add(x);

                            var Loop2 = FD.RoomBoundary.Select(e => e.GetCurve()) as IList<Curve>;
                            var y = CurveLoop.Create(Loop2);
                            Cloop.Add(y);
                            Solid extrusion = GeometryCreationUtilities.CreateExtrusionGeometry(Cloop, XYZ.BasisZ,20);
                            DirectShape directShape = DirectShape.CreateElement(Doc, new ElementId(BuiltInCategory.OST_GenericModel));
                            
                            directShape.SetShape( new List<GeometryObject> { extrusion });

                        }
                        //var DLs = new List<Line>();
                        //if (Drs != null || Drs.Count != 0)
                        //{
                        //    //var Direction = FloorsData[i].DoorHostDirection;
                        //    ////var Points = Drs.Select(d => (d.Location as LocationPoint).Point).ToList();
                        //    //var segment = FloorsData[i].RoomBoundary.Where(s => Math.Abs((s.GetCurve() as Line).Direction.X) ==
                        //    //                                                   Math.Abs(Direction.X)).First();
                        //    DoorsCurve(Drs, DLs);

                        //}




                    }
                    trans.Commit();
                }



                //SpatialElementBoundarySubface.
                Options GeoOp = commandData.Application.Application.Create.NewGeometryOptions();

                GeoOp.View = Doc.ActiveView;
                //using (Transaction trans = new Transaction(Doc, "Create floors from rooms"))
                //{
                //    trans.Start();
                //    for (int i = 0; i < Doors.Count; i++)
                //    {
                //        var D = Doors[i] as FamilyInstance;

                //        var Width = D.Symbol.LookupParameter("Width").AsDouble();
                //        TaskDialog.Show("test", Width.ToString());
                //            var LP = Doors[i].Location as LocationPoint;
                //        var P = LP.Point;
                //        Doc.Create.NewDetailCurve(Doc.ActiveView, Line.CreateBound(P, new XYZ(P.X, P.Y+(Width/2), P.Z)));
                //        //var Lines = (Doors[i].get_Geometry(GeoOp).First() as GeometryInstance).GetSymbolGeometry().Where(e=> e is  Line).Select(e => e as Line).ToList();
                //        //Doc.Create.NewDetailCurveArray(Doc.ActiveView, CreateFloorFromLines.CreateCurveArrayFromLines(Lines));

                //        //var min = Doors[i].get_BoundingBox(Doc.ActiveView).Min;
                //        //var max = Doors[i].get_BoundingBox(Doc.ActiveView).Max;
                //        //Doc.Create.NewDetailCurve(Doc.ActiveView, Line.CreateBound(new XYZ(min.X, min.Y, 0), new XYZ(max.X, max.Y, 0)));
                //    }
                //    // Create the extrusion geometry
                //    //Solid extrusion = GeometryCreationUtilities.CreateExtrusionGeometry(profile, extrusionDirection, extrusionDistance);


                //    trans.Commit();
                //}


                return Result.Succeeded;
            }
            catch (Exception e)
            {

                TaskDialog.Show("Error", e.Message);
                return Result.Failed;
            }
        }
        public static CurveArray CreateCurveArrayFromSegments(List<BoundarySegment> segments)
        {
            CurveArray curveArray = new CurveArray();
            for (int i = 0; i < segments.Count; i++)
            {
                curveArray.Append(segments[i].GetCurve());
            }

            return curveArray;
        }

        public static void DoorsCurve(List<FamilyInstance> Drs,  List<Line> DoorsLines)
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
        public static List<Line> CreateDoorsLoops(Room R , Line L1)
        {
            var DLs = new List<Line>();
            DLs.Add(L1);
            if (Math.Abs(L1.Direction.Y) == 1)
            {
                var X = (R.Location as LocationPoint).Point.X;
                var L2 = Line.CreateBound(L1.GetEndPoint(1),
                                        new XYZ(X, L1.GetEndPoint(1).Y, L1.GetEndPoint(1).Z));
                var L3 = Line.CreateBound(L2.GetEndPoint(1),
                                           new XYZ(L2.GetEndPoint(1).X, L1.GetEndPoint(0).Y, L1.GetEndPoint(0).Z));
                var L4 = Line.CreateBound(L3.GetEndPoint(1), L1.GetEndPoint(0));

                DLs.Add(L2);
                DLs.Add(L3);
                DLs.Add(L4);

            }
            else
            {
                var Y = (R.Location as LocationPoint).Point.Y;
                var L2 = Line.CreateBound(L1.GetEndPoint(1),
                                          new XYZ(L1.GetEndPoint(1).X, Y, L1.GetEndPoint(1).Z));
                var L3 = Line.CreateBound(L2.GetEndPoint(1),
                                         new XYZ(L1.GetEndPoint(0).X, L2.GetEndPoint(1).Y, L2.GetEndPoint(1).Z));
                var L4 = Line.CreateBound(L3.GetEndPoint(1), L1.GetEndPoint(0));

                DLs.Add(L2);
                DLs.Add(L3);
                DLs.Add(L4);
            }

            return DLs;

        }

    }
}
