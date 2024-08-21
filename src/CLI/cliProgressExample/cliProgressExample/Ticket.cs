using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cliProgressExample
{
    public class Ticket
    {
        [Key] // 기본 키로 설정
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // 자동 증가(AUTO_INCREMENT)
        public int Id { get; set; }

        [Required] // 필수 입력
        [MaxLength(100)] // 최대 길이 100
        public string Title { get; set; }

        [MaxLength(500)] // 최대 길이 500
        public string Description { get; set; }

        [DataType(DataType.DateTime)] // 데이터 타입 설정
        public DateTime CreatedDate { get; set; }

        [Required] // 필수 입력
        [MaxLength(50)] // 최대 길이 50
        public string Status { get; set; } // 새로운 속성 추가
    }
}
