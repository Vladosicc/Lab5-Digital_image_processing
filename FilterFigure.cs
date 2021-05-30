using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCOI_5
{
    public interface IFilterFigure
    {
        /// <summary>
        /// Возвращает коэффициент, на который нужно домножать (0 - 1)
        /// x и y - координаты точки
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        double FilterCoef(double x, double y);

        IFilterFigure MirrorFilterFigure(IFilterFigure filter);
    }

    class FilterCircle : IFilterFigure
    {
        public double Ycenter { get; set; }
        public double Xcenter { get; set; }
        public double Radius { get; set; }

        /// <summary>
        /// 1 если точка внутри круга
        /// 0 если точка вне круга
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public double FilterCoef(double x, double y)
        {
            if (Math.Pow(x - Xcenter, 2) + Math.Pow(y - Ycenter, 2) < Radius * Radius)
                return 1;
            else 
                return 0;
        }

        public IFilterFigure MirrorFilterFigure(IFilterFigure filter)
        {
            return new FilterCircle() { Xcenter = -((FilterCircle)filter).Xcenter, Ycenter = -((FilterCircle)filter).Ycenter, Radius = ((FilterCircle)filter).Radius };
        }
    }

    public static class FilterFigureMirror
    {
        public static void AddMirrors(this List<IFilterFigure> filters)
        {
            int Leng = filters.Count;
            for(int i = 0; i < Leng; i++)
            {
                filters.Add(filters[i].MirrorFilterFigure(filters[i]));
            }
        }
    }

    public enum TypeFilterFreq
    {
        HightFreq,
        LowFreq
    }
}
