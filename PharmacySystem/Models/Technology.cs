using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PharmacySystem.Models
{
    public class TechnologyEntry : BaseEntity
    {
        public string MedicineName { get; set; }        // Название формы/препарата
        public TypeOfMeds MedicationType { get; set; }   // Тип (Порошки, Мази и т.д.)
        public string TechName { get; set; }             // Научное название технологии

        /// <summary>
        /// Описание процесса (например: "смешать компоненты, отстоять 2 часа, профильтровать")
        /// </summary>
        public string PreparationMethod { get; set; }    // Технологические стадии (строго по отчету)

        /// <summary>
        /// Время, необходимое на реализацию данной технологии
        /// </summary>
        public TimeSpan StandardProductionTime { get; set; } // Норматив времени
    }
}
