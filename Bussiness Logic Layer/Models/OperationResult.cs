using System;
using System.Collections.Generic;

namespace BusinessLogicLayer.Models
{
    /// <summary>
    /// Універсальна модель результату операції для повернення статусу та даних
    /// </summary>
    public class OperationResult
    {
        /// <summary>Статус успішності операції</summary>
        public bool Success { get; set; }

        /// <summary>Дані результату або повідомлення про помилку</summary>
        public object Data { get; set; }

        /// <summary>
        /// Створює успішний результат операції
        /// </summary>
        public static OperationResult Ok(object data = null)
        {
            return new OperationResult { Success = true, Data = data };
        }

        /// <summary>
        /// Створює невдалий результат операції
        /// </summary>
        public static OperationResult Fail(object data = null)
        {
            return new OperationResult { Success = false, Data = data };
        }
    }
}