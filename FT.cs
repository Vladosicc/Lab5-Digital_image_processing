using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.IntegralTransforms;
using SCOI_5.FFTW;

namespace SCOI_5
{
    /// <summary>
    /// Функции преобразований
    /// </summary>
    public class FT
    {
        public static Complex[] DFT(Complex[] input)
        {
            Complex[] resp = new Complex[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                for (int j = 0; j < input.Length; j++)
                {
                    double u = -2.0 * Math.PI * i * j / input.Length;
                    resp[i] += new Complex(Math.Cos(u), Math.Sin(u)) * input[j];
                }

            }

            return resp;
        }

        public static Complex[] FFT(Complex[] input, int x0, int N, int s)
        {
            Complex[] resp = new Complex[N];
            if (N == 1)
            {
                resp[0] = input[x0];
            }
            else
            {
                FFT(input, x0, N / 2, 2 * s).CopyTo(resp, 0);
                FFT(input, x0 + s, N / 2, 2 * s).CopyTo(resp, N / 2);

                for (int k = 0; k < N / 2; k++)
                {
                    double u = -2.0 * Math.PI * k / N;
                    resp[k] = resp[k] + new Complex(Math.Cos(u), Math.Sin(u)) * resp[k + N / 2];
                    resp[k + N / 2] = resp[k] - new Complex(Math.Cos(u), Math.Sin(u)) * resp[k + N / 2];
                }
            }

            return resp;
        }

        public static Complex[] FFT_2d(Complex[] arr, int width, int height)
        {
            Complex[] resp = new Complex[arr.Length];           

            ParallelOptions opt = new ParallelOptions();
            if (Environment.ProcessorCount > 2)
                opt.MaxDegreeOfParallelism = Environment.ProcessorCount - 1;
            else opt.MaxDegreeOfParallelism = 1;
            for (int i = 0; i < height; ++i)
            //Parallel.For(0, height, opt, i =>
            {
                Complex[] tmp = new Complex[width];
                Complex[] tmp1 = new Complex[width];
                Array.Copy(arr, i * width, tmp1, 0, width);
                Array.Copy(arr, i * width, tmp, 0, width);

                //MathNet.Numerics.IntegralTransforms.Fourier.Forward(tmp1);

                tmp = FFT(tmp, 0, width, 1);

                for (int k = 0; k < width; ++k)
                    resp[i * width + k] = tmp[k] / width;
            }
            //);
            for (int j = 0; j < width; ++j)
            //Parallel.For(0, width, opt, j =>
            {
                Complex[] tmp = new Complex[height];
                for (int k = 0; k < height; ++k)
                    tmp[k] = resp[j + k * width];

                tmp = FFT(tmp, 0, tmp.Length, 1);

                for (int k = 0; k < height; ++k)
                    resp[j + k * width] = tmp[k] / height;
            }
            //);
            return resp;
        }


        public static Complex[] FFTr_2d(Complex[] arr, int width, int height, bool use_FFT = true)
        {
            Complex[] resp = new Complex[arr.Length];

            ParallelOptions opt = new ParallelOptions();
            if (Environment.ProcessorCount > 2)
                opt.MaxDegreeOfParallelism = Environment.ProcessorCount - 1;
            else opt.MaxDegreeOfParallelism = 1;
            for (int i = 0; i < height; ++i)
            //Parallel.For(0, height, opt, i =>
            {
                Complex[] tmp = new Complex[width];
                Array.Copy(arr, i * width, tmp, 0, width);
                for (int k = 0; k < width; ++k)
                    tmp[k] = new Complex(arr[i * width + k].Real, -arr[i * width + k].Imaginary);

                tmp = FFT(tmp, 0, width, 1);

                for (int k = 0; k < width; ++k)
                    resp[i * width + k] = (new Complex(tmp[k].Real, -tmp[k].Imaginary));

            }
            //);

            for (int j = 0; j < width; ++j)
            //Parallel.For(0, width, opt, j =>
            {
                Complex[] tmp = new Complex[height];
                for (int k = 0; k < height; ++k)
                    tmp[k] = new Complex(resp[j + k * width].Real, -resp[j + k * width].Imaginary);

                tmp = FFT(tmp, 0, tmp.Length, 1);

                for (int k = 0; k < height; ++k)
                    resp[j + k * width] = (new Complex(tmp[k].Real, -tmp[k].Imaginary));
            }
            //);
            return resp;
        }
    }
}
