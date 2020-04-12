using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace wdDB.Model
{
    public class Job
    {
        [Key]
        public int Id { get; set; }
        public DateTime Started { get; set; }
        public DateTime Finished { get; set; }
        public DateTime Queued { get; set; }
        public int Status { get; set; }
        public string ErrorMsg { get; set; }
        public string Filename { get; set; }
        public string OFilename { get; set; }
        public string ErrorId { get; set; }
        public double Result_parity { get; set; }
        public double Result_orientation { get; set; }
        public double Result_pixscale { get; set; }
        public double Result_radius { get; set; }
        public double Result_ra { get; set; }
        public double Result_dec { get; set; }
        public string Scale_units { get; set; }
        public string Scale_type { get; set; }
        public double Scale_lower { get; set; }
        public double Scale_upper { get; set; }
        public double Scale_est { get; set; }
        public double Scale_err { get; set; }
        public double Center_ra { get; set; }
        public double Center_dec { get; set; }
        public double Radius { get; set; }
        public double DownsampleFactor { get; set; }
        public double TweakOrder { get; set; }
        public int CrpixCenter { get; set; }
        public double Parity { get; set; }
        public double PositionalError { get; set; }
        public string Url { get; set; }
        public int CancelRequested { get; set; }

        public string ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }



    }
}
