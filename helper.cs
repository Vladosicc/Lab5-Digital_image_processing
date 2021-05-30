using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCOI_5
{
    class _helper
    {
        public static double InBorder(double source, double min = 0, double max = 255)
        {
            if (source > max)
                return max;
            if (source < min)
                return min;
            return source;
        }
    }
}
