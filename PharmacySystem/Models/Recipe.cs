using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmacySystem.Models
{
    /// <summary>
    /// Класс, представляющий рецепт, принесенный пациентом.
    /// Служит входной точкой для формирования заказа.
    /// </summary>
    public class Recipe
    {
        public string PatientFullName { get; set; } = string.Empty;

        public string PatientPhoneNumber { get; set; } = string.Empty;

        // Характеристики требуемого лекарства
        public string MedicationName { get; set; } = string.Empty;
        public TypeOfMeds FormType { get; set; } // Порошок, мазь и т.д.

        /// <summary>
        /// Список ингредиентов, указанных врачом в рецепте (ID компонента и дозировка).
        /// </summary>
        public List<ProductIngredient> PrescribedIngredients { get; set; } = new List<ProductIngredient>();

        public int TotalQuantity { get; set; } = 1; // Количество упаковок/доз
    }
}
