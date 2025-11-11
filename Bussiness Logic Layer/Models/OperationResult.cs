using System;
using System.Collections.Generic;

namespace BusinessLogicLayer.Models
{
    public class OperationResult
    {
        public bool Success { get; set; }
        public object Data { get; set; }

        public static OperationResult Ok(object data = null)
        {
            return new OperationResult { Success = true, Data = data };
        }

        public static OperationResult Fail(object data = null)
        {
            return new OperationResult { Success = false, Data = data };
        }
    }
}
