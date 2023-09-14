using ScottPlot;

namespace BlackStar.View;

/// <summary>
/// DescendendWindow.xaml 的交互逻辑
/// </summary>
public partial class DescendendWindow : Window
{
    public DescendendWindow()
    {
        InitializeComponent();
    }

    public void DrawDescendence(PooledDictionary<int, double> decendend)
    {
        double[] dataX = decendend.Keys.Select(i => (double)i).ToArray();
        double[] dataY = decendend.Values.Select(i =>
                {
                    if (i > 1E8)
                        return 1E8;
                    return i;
                }
            ).ToArray();

        WpfPlot1.Plot.AddScatter(dataX, dataY);
        WpfPlot1.Plot.XLabel("Generation");

        WpfPlot1.Plot.YAxis.DateTimeFormat(true);
        WpfPlot1.Plot.YLabel("Finish");
        WpfPlot1.Plot.YAxis.TickLabelFormat(StringOP.MinutesToTsLabel);
        WpfPlot1.Plot.YAxis.MinimumTickSpacing(0.01);
        WpfPlot1.Plot.YAxis.TickDensity(1);
        //WpfPlot1.Plot.YAxis.AutomaticTickPositions();
        WpfPlot1.Refresh();
    }
}