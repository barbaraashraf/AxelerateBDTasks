using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Task1;

namespace Task3
{
    public class FloorData
    {
        public Room Room { get; set; }
        public List<FamilyInstance> Doors { get; set; }

        public List<Line> DoorsLines { get; set; }
        public List<Curve> TotalBoundry { get; set; }
        public List<BoundarySegment> RoomBoundary { get; set; }

        public FloorData()
        {
            Doors = new List<FamilyInstance>();

            RoomBoundary = new List<BoundarySegment>();
        }
        public void Create_Floor(Autodesk.Revit.DB.Document Doc)
        {
           var x =  CreateFloorFromLines.ReArrange(TotalBoundry.Select(b => b as Line).ToList());
            var CA = new CurveArray();
            for (int i = 0; i < x.Count; i++)
            {
              CA.Append(x[i]);
            }
            Doc.Create.NewFloor(CA, false);
        }
    }
}
