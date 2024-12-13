using System;
using Shuttle.Core.Data;

namespace Shuttle.Esb.Sql.Idempotence;

public interface IQueryFactory
{
    IQuery Create();
    IQuery Register(TransportMessage transportMessage);
    IQuery Handled(TransportMessage transportMessage);
    IQuery Contains(TransportMessage transportMessage);
}