using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics;

namespace Painto.Modules
{
    public class DisplayInfo
    {
        public int ID { get; set; }
        public ulong DisplayId { get; set; }
        public bool IsPrimary { get; set; }
        public RectInt32 WorkArea {  get; set; }
        public SolidColorBrush BackgroundColor; 
    }
}
