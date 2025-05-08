using System;
using System.Data;
using System.IO;
using System.Web.Mvc;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using NCKH.Models;

namespace NCKH.Controllers
{
    public class DeviceController : Controller
    {
        private DatabaseHelper dbHelper = new DatabaseHelper();

        public ActionResult DeviceCode(int id)
        {
            DataTable device = dbHelper.GetDeviceCode(id);
            if (device == null || device.Rows.Count == 0)
                return HttpNotFound();

            string deviceCode = device.Rows[0]["name"].ToString();
            string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);
            string fullUrl = baseUrl + Url.Action("ShowDeviceInfo", "Device", new { deviceCode = deviceCode });

            // -- Dùng QRCoder để tạo QRCode --
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(deviceCode, QRCodeGenerator.ECCLevel.Q);
                using (QRCode qrCode = new QRCode(qrCodeData))
                {
                    using (Bitmap qrCodeImage = qrCode.GetGraphic(20))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            qrCodeImage.Save(ms, ImageFormat.Png);
                            string base64 = Convert.ToBase64String(ms.ToArray());
                            ViewBag.BarcodeImage = "data:image/png;base64," + base64;
                        }
                    }
                }
            }

            return View(device);
        }

        public ActionResult ShowDeviceInfo(string deviceCode)
        {
            DataTable device = dbHelper.GetDeviceInfoByCode(deviceCode);
            if (device == null || device.Rows.Count == 0)
                return HttpNotFound();

            return View(device);
        }
    }
}
