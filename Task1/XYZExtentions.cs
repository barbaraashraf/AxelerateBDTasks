using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Task1
{
    public static class XYZExtentions
    {
        private const double Tolerance = 1e-9;
        public static bool ISEqual (  this XYZ P1, XYZ P2)
        {
          
            if (P1 == null || P2 == null)
            {
                return false; 
            }
            return (System.Math.Abs(P1.X - P2.X) < Tolerance) &&
              (System.Math.Abs(P1.Y - P2.Y) < Tolerance) &&
              (System.Math.Abs(P1.Z - P2.Z) < Tolerance);

            
        }
    }
}
