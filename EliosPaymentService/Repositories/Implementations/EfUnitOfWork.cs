using EliosPaymentService.Models;
using EliosPaymentService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace EliosPaymentService.Repositories.Implementations
{
    public class EfUnitOfWork : IUnitOfWork
    {
        private readonly CVBuilderDataContext _context;
        private readonly IDbContextTransaction _transaction;

        public EfUnitOfWork(CVBuilderDataContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _transaction = _context.Database.BeginTransaction();
        }

        public async Task CommitAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                await _transaction.CommitAsync();
            }
            catch
            {
                await RollbackAsync();
                throw;
            }
        }

        public async Task RollbackAsync()
        {
            await _transaction.RollbackAsync();
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }
    }
}