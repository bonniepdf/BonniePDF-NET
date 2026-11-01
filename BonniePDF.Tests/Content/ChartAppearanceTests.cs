using System.Text;
using Xunit;
namespace BonniePDF.Tests.Content;
public class ChartAppearanceTests
{
    [Fact]
    public async Task ModernChartsUseNativeVectorGradientsAndRoundTrip()
    {
        BarChart chart=new(new ChartData {Title="Quarterly sales",YAxis=new AxisOptions {MaxValue=50},Series=[new ChartSeries {Name="Revenue",DataPoints=[new(){Label="Q1",Value=25},new(){Label="Q2",Value=40},new(){Label="Q3",Value=32}]}]})
        {Theme="modern",Width=480,Height=280,BarStyle=new(){CornerRadius=6,Gradient=new(){StartColor=Color.FromHex("#93c5fd"),EndColor=Color.FromHex("#2563eb"),Scope="plot"}}};
        string svg=chart.ToSvg();Assert.Contains("<linearGradient",svg,StringComparison.Ordinal);Assert.Contains("y=\"38\"",svg,StringComparison.Ordinal);
        using BonniePDF.Document doc=new();doc.Add(chart);
        string definition=doc.ToDefinition().ToJson();
        using BonniePDF.Document copy=await BonniePDF.Document.FromDefinitionAsync(definition);
        Assert.Equal(definition,copy.ToDefinition().ToJson());
        using MemoryStream stream=new();doc.Save(stream,true);
        string text=Encoding.Latin1.GetString(stream.ToArray());
        Assert.Contains("/ShadingType 2",text,StringComparison.Ordinal);Assert.DoesNotContain("/Subtype /Image",text,StringComparison.Ordinal);
    }
}
