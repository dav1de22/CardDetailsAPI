using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Tesseract;
using CardDetailsAPI.Models;
using System.Linq;
using System;
using System.Collections.Generic;


namespace CardDetailsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CardDetailsController : ControllerBase
    {
        [HttpPost("extract-card-details")]
        public IActionResult ExtractCardDetails([FromBody] string imageBase64)
        {
            if (string.IsNullOrEmpty(imageBase64))
            {
                return BadRequest("Invalid image data.");
            }

            try
            {
                // Decode Base64 to Image
                byte[] imageBytes = Convert.FromBase64String(imageBase64);
                using var ms = new MemoryStream(imageBytes);
                using var bitmap = new Bitmap(ms);

                var validate = Base64ToBitmap(imageBase64);

                // Perform OCR using Tesseract
                var extractedText = ExtractTextFromImage(bitmap);

                // Parse extracted text to get card details
                var cardDetails = ParseCardDetails(extractedText);

                // Return card details as JSON
                return Ok(cardDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private string ExtractTextFromImage(byte im)
        {
            string extractedText = string.Empty;

            using (var ms = new MemoryStream())
            {
                using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
                {
                    using (var img = Pix.LoadFromMemory(ms.ToArray()))
                    {
                        using (var page = engine.Process(img))
                        {
                            extractedText = page.GetText();
                        }
                    }
                }
            } 
                return extractedText;
         }

        private string ExtractTextFromImage(Bitmap bitmap)
        {
            string extractedText = string.Empty;

            try
            {
                // Step 1: Convert Bitmap to MemoryStream
                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);  // Save Bitmap as PNG to MemoryStream
                    ms.Position = 0;  // Reset the stream position

                    // Step 2: Load the image into Pix format and process with Tesseract
                    using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
                    {
                        // Load Pix from MemoryStream using Tesseract's Pix.LoadFromMemory
                        using (var pix = Pix.LoadFromMemory(ms.ToArray()))
                        {
                            using (var page = engine.Process(pix))
                            {
                                extractedText = page.GetText();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to extract text from image: {ex.Message}");
            }

            return extractedText;
        }


            private Bitmap Base64ToBitmap(string base64String)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);

            // Validate the byte array to see if it's empty or invalid
            if (imageBytes == null || imageBytes.Length == 0)
            {
                throw new Exception("Error decoding Base64 string: byte array is empty.");
            }

            using (var ms = new MemoryStream(imageBytes))
            {
                Bitmap bitmap;
                try
                {
                    bitmap = new Bitmap(ms);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error creating Bitmap: {ex.Message}");
                }

                return bitmap;
            }
        }

        private CardDetails ParseCardDetails(string text)
        {
            var cardDetails = new CardDetails();

            // Placeholder parsing logic - customize based on card format
            var lines = text.Split('\n');
            foreach (var line in lines)
            {
                if (line.Trim().All(char.IsDigit)) // line.Trim().Length >= 9)"
                {
                    cardDetails.Number = line.Trim();
                }
                else if (line.ToLower().Contains("expiry"))
                {
                    cardDetails.Expiry = line.Split(':').LastOrDefault()?.Trim();
                }
                else
                {
                    var parts = line.Split(' ');
                    if (parts.Length >= 2 && int.TryParse(parts.Last(), out int number))
                    {
                        var name = string.Join(' ', parts.SkipLast(1));
                        cardDetails.People[number] = name;
                    }
                }
            }

            return cardDetails;
        }
    }
}