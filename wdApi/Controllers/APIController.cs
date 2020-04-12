using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Globalization;

namespace wdApi.Controllers
{
    public class ApiFile
    {
        [FromForm(Name = "request-json")]
        public string request_json { get; set; }
        public IFormFile file { get; set; }

    }
    public class APIController : ControllerBase
    {

        private readonly ILogger<APIController> _logger;
        private readonly wdDB.Model.wdDBModel _db;

        public APIController(ILogger<APIController> logger, wdDB.Model.wdDBModel db)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost]
        [Route("api/login")]
        public string Login([FromForm(Name ="request-json")]string request_json)
        {
            string ret = "";
            using (JsonDocument req = JsonDocument.Parse(request_json))
            {
                ret = req.RootElement.GetProperty("apikey").GetRawText();
            }
            string b = "{\"status\": \"success\", \"message\": \"authenticated user: \", \"session\": " + ret + "}";
            return b;
        }

        [HttpPost]
        [Route("api/upload")]
        public async Task<string> Upload([FromForm]ApiFile tf)
        {
            wdDB.Model.Job job = new wdDB.Model.Job();
            job.Started = DateTime.Now;
            job.Status = 0;
            using (JsonDocument req = JsonDocument.Parse(tf.request_json))
            {
                JsonElement e;
                if (req.RootElement.TryGetProperty("center_dec", out e)) job.Center_dec = e.GetDouble();
                if (req.RootElement.TryGetProperty("center_ra", out e)) job.Center_ra = e.GetDouble();
                if (req.RootElement.TryGetProperty("downsample_factor", out e)) job.DownsampleFactor = e.GetDouble();
                if (req.RootElement.TryGetProperty("radius", out e)) job.Radius = e.GetDouble();
                if (req.RootElement.TryGetProperty("scale_lower", out e)) job.Scale_lower = e.GetDouble();
                if (req.RootElement.TryGetProperty("scale_upper", out e)) job.Scale_upper = e.GetDouble();
                if (req.RootElement.TryGetProperty("scale_type", out e)) job.Scale_type = e.GetString();
                if (req.RootElement.TryGetProperty("scale_units", out e)) job.Scale_units = e.GetString();
                if (req.RootElement.TryGetProperty("crpix_center", out e)) job.CrpixCenter= e.GetInt32();
                if (req.RootElement.TryGetProperty("parity", out e)) job.Parity = e.GetDouble();
                if (req.RootElement.TryGetProperty("positional_error", out e)) job.PositionalError = e.GetDouble();
                if (req.RootElement.TryGetProperty("scale_err", out e)) job.Scale_err = e.GetDouble();
                if (req.RootElement.TryGetProperty("scale_est", out e)) job.Scale_est = e.GetDouble();
                if (req.RootElement.TryGetProperty("tweak_order", out e)) job.TweakOrder = e.GetDouble();
            }
            job.Finished = DateTime.MinValue;
            if (tf.file.Length > 0)
            {
                string ext = Path.GetExtension(tf.file.FileName);
                string filePath = Path.GetTempFileName()+ext;
                using (var stream = System.IO.File.Create(filePath))
                {
                    await tf.file.CopyToAsync(stream);
                        
                }
                
                job.OFilename = filePath;
                // TODO: Move to proper location
            }
            _db.Add(job);
            await _db.SaveChangesAsync();
            return "{\"status\": \"success\", \"subid\": " +job.Id.ToString()+", \"hash\": null}";
        }

        [HttpGet]
        [Route("api/submissions/{id}")]
        public string submissions(int id)
        {
            //            { "processing_started": "2016-03-29 11:02:11.967627", "job_calibrations": [[1493115, 785516]],
            //"jobs": [1493115], "processing_finished": "2016-03-29 11:02:13.010625",
            //"user": 1, "user_images": [1051223]
            //    }
            var job = _db.Job.Find(id);
            if (job == null) return "";
            string fin = "None";
            if (job.Finished != DateTime.MinValue) fin = job.Finished.ToString("yyyy-MM-dd HH:mm:ss.fff");
            return "{\"user\": 0, \"user_images\": [], \"processing_started\": \"" + job.Started.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\", \"processing_finished\": \""+fin+"\", \"job_calibrations\": [], \"jobs\": [" + id.ToString() + "] }";
        }
        [HttpGet]
        [Route("api/jobs/{id}")]
        public string jobs(int id)
        {
            var job = _db.Job.Find(id);
            if (job == null) return "";
            string ret = "none";
            if (job.Status == 0) ret = "processing";
            if (job.Status == 1) ret = "solving";
            if (job.Status == 2) ret = "success";
            if (job.Status == 3) ret = "failure";
            return "{\"status\": \"" + ret + "\"}";
        }
        [HttpGet]
        [Route("api/jobs/{id}/calibration")]
        public string jobs_calibration(int id)
        {
            //            { "processing_started": "2016-03-29 11:02:11.967627", "job_calibrations": [[1493115, 785516]],
            //"jobs": [1493115], "processing_finished": "2016-03-29 11:02:13.010625",
            //"user": 1, "user_images": [1051223]
            //    }
            var job = _db.Job.Find(id);
            if (job == null) return "";
            CultureInfo culture;
            culture = CultureInfo.CreateSpecificCulture("en-US");
            return "{\"parity\": " + job.Result_parity.ToString(culture) +", \"orientation\": " + job.Result_orientation.ToString(culture) + ", \"dec\" :"+job.Result_dec.ToString(culture) +", \"ra\": "+job.Result_ra.ToString(culture) +", \"pixscale\": "+job.Result_pixscale.ToString(culture) +", \"radius\": "+job.Result_radius.ToString(culture)+ "}";
        }
    }
}
