using System.Collections.Generic;
using System.Linq;
using BusinessLogicLayer.Models;

namespace BusinessLogicLayer.Services
{
    public class OperatorService
    {
        private List<Operator> _operators;
        private int _nextId = 1;

        public OperatorService()
        {
            _operators = new List<Operator>();
        }

        public List<Operator> GetAll() => _operators;

        public Operator GetById(int id) => _operators.FirstOrDefault(o => o.Id == id);

        public OperationResult Add(string name)
        {
            var op = new Operator(name)
            {
                Id = _nextId++
            };
            _operators.Add(op);
            return OperationResult.Ok(op);
        }

        public OperationResult Delete(int id)
        {
            var op = GetById(id);
            if (op == null) return OperationResult.Fail();

            _operators.Remove(op);
            return OperationResult.Ok();
        }

        public void LoadOperators(List<Operator> operators)
        {
            _operators = operators ?? new List<Operator>();
            _nextId = _operators.Any() ? _operators.Max(o => o.Id) + 1 : 1;
        }
    }
}