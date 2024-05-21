using BlackStar.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BlackStar.View;

/// <summary>
/// DescendendWindow.xaml 的交互逻辑
/// </summary>
public partial class DescendendWindow : Window
{
    private List<int> dataX = new();
    private List<double> dataY = new();
    public DescendendWindow()
    {
        InitializeComponent();

        initDrawDescendence();
    }

    private void initDrawDescendence()
    {
        //double[] dataX = this.dataX.Select(i => (double)i).ToArray();
        //double[] dataY = this.dataY.Select(i =>
        //        {
        //            if (i > 1E8)
        //                return 1E8;
        //            return i;
        //        }
        //    ).ToArray();

        //WpfPlot1.Plot.AddScatter(dataX, dataY);
        WpfPlot1.Plot.XLabel("Generation");

        WpfPlot1.Plot.YAxis.DateTimeFormat(true);
        WpfPlot1.Plot.YLabel("Finish");
        WpfPlot1.Plot.YAxis.TickLabelFormat(StringOP.MinutesToTsLabel);
        WpfPlot1.Plot.YAxis.MinimumTickSpacing(0.01);
        WpfPlot1.Plot.YAxis.TickDensity(1);
        //WpfPlot1.Plot.YAxis.AutomaticTickPositions();
        WpfPlot1.Refresh();
    }

    public void AddDraw(int x, TimeSpan y)
    {
        var yMinutes = y.TotalMinutes;

        if (this.dataX.Count == 0)
            WpfPlot1.Plot.AddScatter([x], [yMinutes]);
        else
            WpfPlot1.Plot.AddLine((double)this.dataX.Last(), this.dataY.Last(), (double)x, yMinutes);
        this.dataX.Add(x);
        this.dataY.Add(yMinutes);

        WpfPlot1.Plot.AxisAuto();
        WpfPlot1.Refresh();
    }

    private void BtFit_OnClick(object sender, RoutedEventArgs e)
    {
        WpfPlot1.Plot.AxisAuto();
        WpfPlot1.Refresh();
    }
}