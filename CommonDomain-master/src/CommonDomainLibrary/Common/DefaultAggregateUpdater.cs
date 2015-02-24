using System;
using System.Threading.Tasks;
using Edit;
using NLog;

namespace CommonDomainLibrary.Common
{
    public class DefaultAggregateUpdater : IAggregateUpdater
    {
        private readonly IAggregateRepository _aggregateRepository;
        private readonly IBus _bus;
        private readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public DefaultAggregateUpdater(IAggregateRepository aggregateRepository, IBus bus)
        {
            _aggregateRepository = aggregateRepository;
            _bus = bus;
        }

        public async Task Update<T>(Guid id, Guid causationId, Action<T> action, bool lastTry, bool onlyExistingAggregates = false)
            where T : class, IAggregate, IMessageAccessor
        {
            await Update<T>(id, causationId, e => Task.Run(() => action(e)), lastTry, onlyExistingAggregates);
        }

        public async Task Update<T>(Guid id, Guid causationId, Func<T, Task> action, bool lastTry, bool onlyExistingAggregates = false) where T : class, IAggregate, IMessageAccessor
        {
            var retries = 0;            
            while (true)
            {
                try
                {
                    IErrorEvent errorEvent = null;
                    var response = await _aggregateRepository.GetById<T>(id);
                    var aggregate = response.Aggregate as T;

                    if (aggregate == null)
                        throw DomainError.Named("aggregate-repository-response-error", true,
                                                "The aggregate component of the response is not an aggregate");

                    if (aggregate.Id == Guid.Empty && onlyExistingAggregates) return;

                    if (!aggregate.Messages.HasCausation(causationId))
                    {
                        try
                        {
                            Logger.Info("BEGIN: AggregateUpdater for aggregate '{0}' executing action",
                                                aggregate.GetType().Name);
                            await action(aggregate);
                            Logger.Info("END: AggregateUpdater for aggregate '{0}' executing action",
                                                aggregate.GetType().Name);
                        }
                        catch (DomainError ex)
                        {
                            Logger.ErrorException(string.Format(
                                "AggregateUpdater for aggregate '{0}' action threw DomainError", aggregate.GetType().Name), ex);
                            if (lastTry || !ex.Retry)
                            {
                                errorEvent = ex.ErrorEvent;
                            }
                            else throw;
                        }

                        if (errorEvent != null)
                        {
                            Logger.Info(
                                    "AggregateUpdater for aggregate '{0}' publishing error event '{1}'",
                                    aggregate.GetType().Name, errorEvent.GetType().Name);
                            await _bus.Publish(errorEvent);
                            return;
                        }

                        Logger.Info("BEGIN: AggregateUpdater saving aggregate '{0}' state",
                                            aggregate.GetType().Name);
                        if (aggregate.Messages.HasCausation(causationId))
                        {
                            if (aggregate.Id == Guid.Empty)
                                throw DomainError.Named("aggregate-not-created", true,
                                                        "Tried to save changes to an aggregate that has not been created first");
                            await _aggregateRepository.Save(causationId, aggregate, response.Version);
                        }
                        Logger.Info("END: AggregateUpdater saving aggregate '{0}' state",
                                            aggregate.GetType().Name);
                    }

                    Logger.Info("BEGIN: AggregateUpdater publishing aggregate '{0}' events",
                                        aggregate.GetType().Name);
                    foreach (var e in aggregate.Messages.GetEvents(causationId))
                    {
                        await _bus.Publish(e);
                    }
                    Logger.Info("END: AggregateUpdater publishing aggregate '{0}' events",
                                        aggregate.GetType().Name);

                    Logger.Info("BEGIN: AggregateUpdater publishing aggregate '{0}' commands",
                                        aggregate.GetType().Name);
                    foreach (var c in aggregate.Messages.GetCommands(causationId))
                    {
                        await _bus.Publish(c);
                    }
                    Logger.Info("END: AggregateUpdater publishing aggregate '{0}' commands",
                                        aggregate.GetType().Name);

                    Logger.Info("BEGIN: AggregateUpdater publishing aggregate '{0}' deferred commands",
                                        aggregate.GetType().Name);
                    foreach (var c in aggregate.Messages.GetDeferredCommands(causationId))
                    {
                        await _bus.Defer(c.Message, c.Instant);
                    }
                    Logger.Info("END: AggregateUpdater publishing aggregate '{0}' deferred commands",
                                        aggregate.GetType().Name);

                    break;
                }
                catch (DomainError)
                {
                    throw;
                }
                catch (ConcurrencyException ex)
                {
                    Logger.ErrorException("AggregateUpdater got concurrency exception", ex);
                    if (retries < 3) retries++;
                    else
                    {
                        throw DomainError.Named("domain-concurrency-exception", true, "Exceeded maximum number of retries");
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("AggregateUpdater got other exception", ex);
                    throw;
                }
            }
        }
    }
}