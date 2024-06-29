using Collections.Pooled;
using ScottPlot.Plottables;
using static log4net.Appender.FileAppender;

namespace BlackStar.View;

/// <summary>
/// DescendendWindow.xaml 的交互逻辑
/// </summary>
public partial class DescendendWindow : Window
{
    //ScottPlot.Plottables.DataStreamer Streamer1;
    public DescendendWindow()
    {
        InitializeComponent();
       
        initDrawDescendence();
    }

    private void initDrawDescendence()
    {
        
        WpfPlot1.Plot.XLabel("Generation");
        WpfPlot1.Plot.YLabel("Finish ( minutes )");
        //Streamer1 = WpfPlot1.Plot.Add.DataStreamer(20);
        WpfPlot1.Refresh();
    }

    PooledList<double> xList = new();
    PooledList<double> yList = new();
    private Scatter scatter;
    public void AddDraw(int x, TimeSpan y)
    {
        xList.Add(x);
        yList.Add(y.TotalMinutes);
        double[] xs = xList.ToArray();
        double[] ys = yList.ToArray();
        if(scatter!=null)
            WpfPlot1.Plot.Remove(scatter);
        scatter = WpfPlot1.Plot.Add.Scatter(xs, ys);
        scatter.MarkerShape = MarkerShape.FilledSquare;
        scatter.MarkerSize = 5;
        scatter.LinePattern = LinePattern.DenselyDashed;
        scatter.LineWidth = 1.5f;
        scatter.Smooth = true;
        scatter.SmoothTension = 2f;
        WpfPlot1.Plot.Axes.AutoScale();
        WpfPlot1.Refresh();
    }

    private void BtFit_OnClick(object sender, RoutedEventArgs e)
    {
        WpfPlot1.Plot.Axes.AutoScale();
        WpfPlot1.Refresh();
    }
}