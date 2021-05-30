using Exocortex.DSP;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SCOI_5
{
    public class MyComplex
    {
        public double Re;
        public double Im;

        public MyComplex() { }
        public MyComplex(double re, double im) { Re = re; Im = im; }
    }

    public class ImgFT
    {
        static byte clampByte(double d)
        {
            if (d > 255)
                return 255;
            if (d < 0)
                return 0;
            return (byte)d;
        }

        public ImgFT()
        {
        }

        public static ResultImages Calculate(BitmapSource source, List<IFilterFigure> filterFigures = null, TypeFilterFreq typeFilter = TypeFilterFreq.HightFreq, int threadUse = 1)
        {
            if (source == null)
            {
                return new ResultImages() { FilterImage = null, FourierImage = null, NewImage = null };
            }

            int _stride;
            int _height;
            int _width;
            PixelFormat _pixelFormat;
            int _bitsPerPixel;

            int realHeight = source.PixelHeight;
            int realWidth = source.PixelWidth;

            //в степень двойки
            int newHeight = realHeight;
            int newWidth = realWidth;

            double l = Math.Log(realWidth, 2);
            if (l != Math.Floor(l))
            {
                newWidth = (int)Math.Pow(2, Math.Ceiling(l));
            }
            l = Math.Log(realHeight, 2);
            if (l != Math.Floor(l))
            {
                newHeight = (int)Math.Pow(2, Math.Ceiling(l));
            }

            byte[] oldBytes = source.Resize(newWidth, newHeight).ToByte(out _stride, out _height, out _width, out _pixelFormat);
            _bitsPerPixel = _pixelFormat.BitsPerPixel;
            int ink = _bitsPerPixel / 8;

            byte[] newBytes = new byte[oldBytes.Length];
            byte[] fourBytes = new byte[oldBytes.Length];
            byte[] fourFilterBytes = new byte[oldBytes.Length];

            //Закидываем альфаканал
            if (_bitsPerPixel == 32)
            {
                for (int i = 0; i < newBytes.Length; i += ink)
                {
                    newBytes[i + 3] = fourBytes[i + 3] = fourFilterBytes[i + 3] = (byte)255;
                }
            }

            ParallelOptions opt = new ParallelOptions();
            if (threadUse > Environment.ProcessorCount)
                opt.MaxDegreeOfParallelism = Environment.ProcessorCount;
            else opt.MaxDegreeOfParallelism = threadUse;

            bool[] _mapFilter = null;
            //Просчитаем филтр-карту, чтобы 3 раза не пересчитывать
            //Работает только для идеальных, для не идельных надо по другому
            if (filterFigures != null)
            {
                filterFigures.AddMirrors();
                _mapFilter = new bool[newWidth * newHeight];
                int centerX = newWidth / 2;
                int centerY = newHeight / 2;
                Parallel.For(0, newWidth * newHeight, opt, (i) =>
                {
                    _mapFilter[i] = true;
                    bool inFigure = false;
                    int y = i / newWidth;
                    int x = i % newWidth;
                    for (int filterCount = 0; filterCount < filterFigures.Count; filterCount++)
                    {
                        if (filterFigures[filterCount].FilterCoef(x - centerX, centerY - y) == 1)
                        {
                            inFigure = true;
                            break;
                        }
                    }
                    switch (typeFilter)
                    {
                        case TypeFilterFreq.HightFreq:
                            {
                                if (inFigure)
                                {
                                    _mapFilter[i] = false;
                                }
                                else
                                {
                                    fourFilterBytes[i * ink] = 255;
                                    fourFilterBytes[i * ink + 1] = 255;
                                    fourFilterBytes[i * ink + 2] = 255;
                                }
                                break;
                            }
                        case TypeFilterFreq.LowFreq:
                            {
                                if (!inFigure)
                                {
                                    _mapFilter[i] = false;
                                }
                                else
                                {
                                    fourFilterBytes[i * ink] = 255;
                                    fourFilterBytes[i * ink + 1] = 255;
                                    fourFilterBytes[i * ink + 2] = 255;
                                }
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }

                });
            }

            float scale = 1f / (float)Math.Sqrt(newWidth * newHeight);

            ParallelOptions optForChannel = new ParallelOptions();
            optForChannel.MaxDegreeOfParallelism = 3;

            Parallel.For(0, 3, optForChannel, (channel) =>
            {
                ComplexF[] complex_bytes2 = new ComplexF[newWidth * newHeight];
                Parallel.For(0, newWidth * newHeight, opt, (i) =>
                {
                    int y = i / newWidth;
                    int x = i - y * newWidth;
                    complex_bytes2[i] = Math.Pow(-1, x + y) * oldBytes[i * ink + channel];
                });

                //Преобразование
                Fourier.FFT2(complex_bytes2, newWidth, newHeight, FourierDirection.Forward, threadUse);
                Parallel.For(0, newWidth * newHeight, opt, (i) =>
                {
                    complex_bytes2[i] *= scale;
                });

                ComplexF[] complex_bytes_filtered = complex_bytes2.Clone() as ComplexF[];
                //Сюда фильтры
                if (filterFigures != null)
                {
                    Parallel.For(0, newWidth * newHeight, opt, (i) =>
                    {
                        if (!_mapFilter[i])
                        {
                            complex_bytes_filtered[i] = 0;
                        }
                    });
                }

                //Обратное преобразование
                var complex_bytes_res = complex_bytes_filtered.Clone() as ComplexF[];
                Fourier.FFT2(complex_bytes_res, newWidth, newHeight, FourierDirection.Backward, threadUse);
                Parallel.For(0, newWidth * newHeight, opt, (i) =>
                {
                    complex_bytes_res[i] *= scale;
                });

                //
                Parallel.For(0, newWidth * newHeight, opt, (i) =>
                {
                    int y = i / newWidth;
                    int x = i - y * newWidth;
                    y -= newHeight / 2;
                    x -= newWidth / 2;
                    newBytes[i * ink + channel] = clampByte(Math.Round((Math.Pow(-1, x + y) * complex_bytes_res[i]).Re));
                    fourBytes[i * ink + channel] = clampByte(complex_bytes2[i].GetModulus());
                });
            });

            #region Старый код
            //for (int channel = 0; channel <= 2; channel++)
            //{
            //    ComplexF[] complex_bytes2 = new ComplexF[newWidth * newHeight];
            //    for (int i = 0; i < newWidth * newHeight; ++i)
            //    {
            //        int y = i / newWidth;
            //        int x = i - y * newWidth;
            //        complex_bytes2[i] = Math.Pow(-1, x + y) * oldBytes[i * ink + channel];
            //    }

            //    //Преобразование
            //    Fourier.FFT2(complex_bytes2, newWidth, newHeight, FourierDirection.Forward, threadUse);
            //    for (int i = 0; i < complex_bytes2.Length; i++)
            //    {
            //        complex_bytes2[i] *= scale;
            //    }


            //    ComplexF[] complex_bytes_filtered = complex_bytes2.Clone() as ComplexF[];
            //    //Сюда фильтры
            //    if (filterFigures != null)
            //    {
            //        for (int i = 0; i < complex_bytes2.Length; i++)
            //        {
            //            if(!_mapFilter[i])
            //            {
            //                complex_bytes_filtered[i] = 0;
            //            }
            //        }
            //    }

            //    //Обратное преобразование
            //    var complex_bytes_res = complex_bytes_filtered.Clone() as ComplexF[];
            //    Fourier.FFT2(complex_bytes_res, newWidth, newHeight, FourierDirection.Backward, threadUse);
            //    for (int i = 0; i < complex_bytes_res.Length; i++)
            //    {
            //        complex_bytes_res[i] *= scale;
            //    }

            //    //
            //    for (int i = 0; i < newWidth * newHeight; ++i)
            //    {
            //        int y = i / newWidth;
            //        int x = i - y * newWidth;
            //        y -= newHeight / 2;
            //        x -= newWidth / 2;
            //        newBytes[i * ink + channel] = clampByte(Math.Round((Math.Pow(-1, x + y) * complex_bytes_res[i]).Re));
            //        fourBytes[i * ink + channel] = clampByte(complex_bytes2[i].GetModulus());
            //    }
            //}
            #endregion

            //Если надо в серый
            //byte tmp = 0;
            //BinaryCPP.ToGray(fourBytes, fourBytes.Length, _bitsPerPixel, ref tmp);

            return new ResultImages() { NewImage = newBytes.ToBitmapSource(_stride, _width, _height, _pixelFormat).Resize(realWidth, realHeight), FourierImage = fourBytes.ToBitmapSource(_stride, _width, _height, _pixelFormat), FilterImage = fourFilterBytes.ToBitmapSource(_stride, _width, _height, _pixelFormat) };
        }
    }

    public class ResultImages
    {
        public BitmapSource NewImage { get; set; }
        public BitmapSource FourierImage { get; set; }
        public BitmapSource FilterImage { get; set; }
    }
}
