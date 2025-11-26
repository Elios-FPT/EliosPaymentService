using EliosPaymentService.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EliosPaymentService.Repositories.Implementations
{
    public class CombinedTransaction : ICombinedTransaction
    {
        public IKafkaTransaction KafkaTransaction { get; }
        public IUnitOfWork DbTransaction { get; }

        public CombinedTransaction(IKafkaTransaction kafkaTransaction, IUnitOfWork dbTransaction)
        {
            KafkaTransaction = kafkaTransaction;
            DbTransaction = dbTransaction;
        }

        public void Dispose()
        {
            KafkaTransaction?.Dispose();
            DbTransaction?.Dispose();
        }
    }
}
