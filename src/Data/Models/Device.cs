namespace iPhoneController.Data.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("device")]
    public class Device
    {
        [Key, Column("uuid")]
        public string Uuid { get; set; }

        [Column("name")]
        public string Name { get; set; }
    }
}