using System;
using System.ComponentModel.DataAnnotations;

namespace WmsSystem.Models
{
    public class SystemLog
    {
        public int Id { get; set; }
        
        public DateTime Ts { get; set; } = DateTime.Now;
        
        [StringLength(100)]
        public string UserName { get; set; }
        
        [StringLength(50)]
        public string Ip { get; set; }
        
        [StringLength(100)]
        public string Controller { get; set; }
        
        [StringLength(100)]
        public string Action { get; set; }
        
        [StringLength(10)]
        public string HttpMethod { get; set; }
        
        [StringLength(500)]
        public string Url { get; set; }
        
        public string Params { get; set; }
        
        [StringLength(50)]
        public string Result { get; set; }
        
        public int? DurationMs { get; set; }
    }
}
