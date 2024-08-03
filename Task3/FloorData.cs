using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task3
{
    public class FloorData
    {
       public Room Room { get; set; }
        public List<FamilyInstance> Doors { get; set; }
        
        public List<Line> DoorsLines { get; set; }
     
        public List<BoundarySegment>  RoomBoundary { get; set; }
       
        public FloorData()
        {
            Doors = new List<FamilyInstance>();
           
            RoomBoundary = new List<BoundarySegment>();
        }
    }
}
