using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tesseract;

namespace ArimartEcommerceAPI.Services.Services
{
    public static class OcrHelper
    {
        public static string ExtractText(string imagePath)
        {
            string tessData = Path.Combine(Directory.GetCurrentDirectory(), "tessdata");
            using (var engine = new Tesseract.TesseractEngine(tessData, "eng", Tesseract.EngineMode.Default))
            {
                using (var img = Pix.LoadFromFile(imagePath))
                {
                    using (var page = engine.Process(img))
                    {
                        return page.GetText();
                    }
                }
            }
        }
    }

}
