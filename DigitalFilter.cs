using static System.Math;
using System.Collections.Generic;

namespace DigitalFilter
{
    /// <summary>
    /// 双二次変換を用いたデジタルフィルタ
    /// </summary>
    public class DigitalFilter
    {
        private double fa = 0.0f;
        private double pfc = 0.0f;
        private double in1 = 0.0f;
        private double in2 = 0.0f;
        private double out1 = 0.0f;
        private double out2 = 0.0f;
        private Queue<double> buffer;
        public readonly double omega;
        public readonly double [] z;
        public readonly double alpha;
        public readonly double internalBandWidth;
        public readonly double q;
        public readonly double a0;
        public readonly double a1;
        public readonly double a2;
        public readonly double b0;
        public readonly double b1;
        public readonly double b2;
        public readonly int averageNum;
        public readonly FilterType internalFilter;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="secControl">制御周期</param>
        /// <param name="cutoff">カットオフ周波数</param>
        /// <param name="filterType">フィルターの種類</param>
        /// <param name="bandWidth">帯域幅(Default = 1octave)</param>
        public DigitalFilter(double secControl, double cutoff, FilterType filterType, double bandWidth = 1.0f)
        {
            z = new double[4];
            internalBandWidth = bandWidth;
            internalFilter = filterType;
            omega = 2.0f * PI * cutoff / secControl;
            q = 1.0f / Sqrt(2);
            fa = 1.0 / (2.0 * PI) * Tan(PI * cutoff / secControl);
            pfc = 2.0 * PI * fa;

            switch (internalFilter)
            {
                case FilterType.LowPassFilter:
                    //alpha = Sin(omega) / (2.0f * q);
                    //a0 = 1.0f + alpha;
                    //a1 = -2.0f * Cos(omega);
                    //a2 = 1.0f - alpha;
                    //b0 = (1.0f - Cos(omega)) / 2.0f;
                    //b1 = 1.0f - Cos(omega);
                    //b2 = (1.0f - Cos(omega)) / 2.0f;

                    alpha = Sin(omega) / (2.0f * q);
                    b0 = 1.0f;
                    b1 = (-2.0 + 2.0 * pfc * pfc) / (1 + Sqrt(2) * pfc + pfc * pfc);
                    b2 = (1.0 - Sqrt(2) * pfc + pfc * pfc) / (1 + Sqrt(2) * pfc + pfc * pfc);

                    a0 = pfc * pfc / (1 + Sqrt(2) * pfc + pfc * pfc);
                    a1 = 2.0 * pfc * pfc / (1 + Sqrt(2) * pfc + pfc * pfc);
                    a2 = pfc * pfc / (1 + Sqrt(2) * pfc + pfc * pfc);

                    break;

                case FilterType.HighPassFilter:
                    alpha = Sin(omega) / (2.0f * q);
                    a0 = 1.0f + alpha;
                    a1 = -2.0f * Cos(omega);
                    a2 = 1.0f - alpha;
                    b0 = (1.0 + Cos(omega)) / 2.0f;
                    b1 = -(1.0f + Cos(omega));
                    b2 = (1.0f + Cos(omega)) / 2.0f;
                    break;

                case FilterType.BandPassFilter:
                    alpha = Sin(omega) * Sinh(Log(2)) /
                                2.0f * internalBandWidth * omega / Sin(omega);
                    a0 = 1.0f + alpha;
                    a1 = -2.0f * Cos(omega);
                    a2 = 1.0f - alpha;
                    b0 = alpha;
                    b1 = 0.0f;
                    b2 = -alpha;
                    break;

                case FilterType.BandStopFilter:
                    alpha = Sin(omega) * Sinh(Log(2)) /
                                2.0f * internalBandWidth * omega / Sin(omega);
                    a0 = 1.0f + alpha;
                    a1 = -2.0f * Cos(omega);
                    a2 = 1.0f - alpha;
                    b0 = 1.0f;
                    b1 = -2.0f * Cos(omega);
                    b2 = 1.0f;
                    break;

                case FilterType.AllPassFilter:
                    alpha = Sin(omega) / (2.0f * q);
                    a0 = 1.0f + alpha;
                    a1 = -2.0f * Cos(omega);
                    a2 = 1.0f - alpha;
                    b0 = 1.0f - alpha;
                    b1 = -2.0f * Cos(omega);
                    b2 = 1.0f + alpha;
                    break;

                case FilterType.MovingAverageFilter:
                    buffer = new Queue<double>();
                    averageNum = (int)cutoff;
                    break;
            }
        }
        /// <summary>
        /// 入力値に対するフィルタ適用値を返す
        /// </summary>
        /// <param name="input">フィルタ対象値</param>
        /// <returns>フィルタ適用値</returns>
        public double FilterControl(double input)
        {
            double output;
            switch (internalFilter)
            {
                case FilterType.MovingAverageFilter:
                    buffer.Enqueue(input);
                    double sum = 0.0f;
                    if (buffer.Count > averageNum)
                    {
                        buffer.Dequeue();
                    }
                    foreach (double data in buffer)
                    {
                        sum += data;
                    }
                    return (sum / averageNum);

                case FilterType.LowPassFilter:
                     output = input * a0 + z[0] * a1 + z[1] * a2
                            - z[2] * b1 - z[3] * b2;
                    z[1] = z[0];
                    z[0] = input;
                    z[3] = z[2];
                    z[2] = output;

                    return output;

                default:
                    output = b0 / a0 * input +
                                            b1 / a0 * in1 +
                                            b2 / a0 * in2 -
                                            a1 / a0 * out1 -
                                            a2 / a0 * out2;
                    in2 = in1;
                    in1 = input;
                    out2 = out1;
                    out1 = output;
                    return output;
            }
        }
        /// <summary>
        /// キューバッファをクリアする
        /// </summary>
        public void BufferClear()
        {
            if (buffer != null)
            {
                buffer.Clear();
            }
        }
    }
}