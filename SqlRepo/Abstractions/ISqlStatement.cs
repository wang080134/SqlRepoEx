using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SqlRepoEx.Abstractions
{
    public interface ISqlStatement<TResult> : IClauseBuilder
    {
        string TableName { get; }
        string TableSchema { get; }
        TResult Go();
        Task<TResult> GoAsync();

        ISqlStatement<TResult> UseConnectionProvider(IConnectionProvider connectionProvider);
    }
}