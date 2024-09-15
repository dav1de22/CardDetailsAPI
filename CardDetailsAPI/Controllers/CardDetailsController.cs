using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Tesseract;
using CardDetailsAPI.Models;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.RegularExpressions;


namespace CardDetailsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CardDetailsController : ControllerBase
    {
        [HttpPost("extract-card-details")]
        public IActionResult ExtractCardDetails([FromBody] string imageBase64)
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="imageBase64">The input string containing the picture of a medicare card in Base64 format.</param>
            /// <returns>The extrapolated data from the image as an object containg a card number, list of people in the card and expiry.</returns>
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

                // Parse extracted text to get card details Numbers, People and Expiry Date
                var cardDetails = ParseCardDetails(extractedText);
                
                // Return card details 
                return Ok(cardDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private string ExtractTextFromImage(Bitmap bitmap)
        {
            string extractedText = string.Empty;

            try
            {
                // Step 1: Convert Bitmap to MemoryStream
                using (var ms = new MemoryStream())
                {
                    // Save Bitmap as PNG to MemoryStream, PNG helps with the OCR
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);  
                    // Reset the stream position
                    ms.Position = 0;  

                    // Step 2: Load the engine and the language pack neccesary for OCR with Tesseract
                    using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
                    {
                    // Load the stream to pix, page gets the processed text and assigns it to extracted text string
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

            try
            {
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
            catch (Exception ex)
            {
                throw new Exception($"Failed to convert base64 to Bitmap: {ex.Message}");
            }
        
        }
        private CardDetails ParseCardDetails(string text)
        {
            try
            {
                var cardDetails = new CardDetails();
                // Dictionary to hold the number and corresponding name
                Dictionary<int, string> nameDict = new Dictionary<int, string>();

                var expiryRegex = new Regex(@"\b\d{2}/\d{2}\b");

                // Placeholder parsing logic - customize based on card format
                string[] lines = text.Split('\n').Select(line => line.Trim()).ToArray();

                foreach (var line in lines)
                {
                    if (line.Trim().Length >= 9)
                    {
                        string[] cardNumberParts = line.Replace('o', '0').Replace('O', '0').Split(' ');
                        string cardNumber = string.Join("", cardNumberParts).Substring(0, 10);

                        if (cardNumber.All(char.IsDigit) && cardNumber.Length >= 9)
                        {
                            cardDetails.Number = cardNumber;
                            break;
                        }
                    }
                }


                // Process each line containing names
                foreach (string line in lines)
                {
                    if (line.Length < 1)
                    {
                        continue;
                    }

                    //line.Replace("  "," ");   

                    string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (expiryRegex.IsMatch(line))
                    {
                        cardDetails.Expiry = expiryRegex.Match(line).Value;
                        continue;
                    }
                    // Check if the line has exactly three parts: number, firstname, and lastname
                    if (parts.Length != 3) continue;

                    // Try to parse the first part as the number
                    if (!int.TryParse(parts[0], out int number)) continue;

                    if (parts[1].All(char.IsDigit)) continue;
                    // Construct the full name from the second and third parts
                    string fullName = parts[1] + " " + parts[2];

                    // Add to the dictionary
                    nameDict[number] = fullName;
                }


                cardDetails.People = nameDict;

                var cardInfo = new CardDetails
                {
                    Number = cardDetails.Number,
                    People = cardDetails.People,
                    Expiry = cardDetails.Expiry
                };




                return cardDetails;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse text to CardDetails: {ex.Message}");
            }
        }



    }
}
