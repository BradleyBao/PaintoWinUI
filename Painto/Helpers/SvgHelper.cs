using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Data.Xml.Dom;
using Windows.Storage.Streams;
using System.IO;
using System.Threading.Tasks;
using System;


namespace Painto.Helpers
{
    public static class SvgHelper
    {
        public static async Task ChangeSvgColorAsync(Image image, string svgPath, string newColor)
        {
            string svgContent = File.ReadAllText(svgPath);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(svgContent);

            var elements = xmlDoc.GetElementsByTagName("path");
            foreach (var element in elements)
            {
                var attr = element.Attributes.GetNamedItem("fill");
                if (attr != null)
                {
                    attr.NodeValue = newColor;
                }
            }

            var modifiedSvgContent = xmlDoc.GetXml();
            var svgImageSource = new SvgImageSource();
            using (var memoryStream = new InMemoryRandomAccessStream())
            {
                using (var writer = new DataWriter(memoryStream))
                {
                    writer.WriteString(modifiedSvgContent);
                    await writer.StoreAsync();
                    memoryStream.Seek(0);
                    await svgImageSource.SetSourceAsync(memoryStream);
                }
            }

            image.Source = svgImageSource;
        }
    }


}
