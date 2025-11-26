using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EliosPaymentService.Models;
using EliosPaymentService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EliosPaymentService.Repositories.Implementations;

public class Repository<TEntity>(CVBuilderDataContext context) : IRepository<TEntity>
    where TEntity : class
{
    protected CVBuilderDataContext Context { get; } = context;
    protected DbSet<TEntity> Set => Context.Set<TEntity>();

    public virtual async Task<TEntity?> GetByIdAsync(int id) =>
        await Set.FindAsync(id);

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync() =>
        await Set.AsNoTracking().ToListAsync();

    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        await Set.AddAsync(entity);
        await Context.SaveChangesAsync();
        Context.Entry(entity).State = EntityState.Detached;
        return entity;
    }

    public virtual async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities)
    {
        var list = entities.ToList();
        await Set.AddRangeAsync(list);
        await Context.SaveChangesAsync();
        foreach (var entity in list)
        {
            Context.Entry(entity).State = EntityState.Detached;
        }

        return list;
    }

    public virtual async Task UpdateAsync(TEntity entity)
    {
        Set.Update(entity);
        await Context.SaveChangesAsync();
        Context.Entry(entity).State = EntityState.Detached;
    }

    public virtual async Task DeleteAsync(TEntity entity)
    {
        Set.Remove(entity);
        await Context.SaveChangesAsync();
    }

    public virtual async Task<bool> ExistsAsync(int id) =>
        await Set.FindAsync(id) is not null;

    public virtual Task<int> SaveChangesAsync() => Context.SaveChangesAsync();
}

