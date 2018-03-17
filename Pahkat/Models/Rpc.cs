using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Pahkat.Util;
using System.Threading;

namespace Pahkat.Rpc
{
    struct Request
    {
        public string Method;
        public object Params;
    }

    struct SubscriptionRequest<T>
    {
        public string Method;
        public object Params;
        public Func<T, bool> CompletionFilter;
    }

    struct RawCallbackParams<T>
    {
        [JsonProperty("result")]
        public T Result;
        [JsonProperty("subscription")]
        public object Subscription;
    }

    struct RawCallback<T>
    {
        [JsonProperty("method")]
        public string Method;
        [JsonProperty("params")]
        private RawCallbackParams<T> _params;

        public T Result => _params.Result;
        public object Subscription => _params.Subscription;

    }

    struct RawErrorCallbackParams
    {

    }

    struct RawErrorCallback
    {

    }

    class RawResponse<T>
    {
        [JsonProperty("id", Required = Required.Always)]
        public int Id;

        [JsonProperty("error")]
        public RpcError Error;

        [JsonProperty("result")]
        public T Result;
    }

    public class RpcError
    {
        [JsonProperty("code")]
        public int Code { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }
    }

    public class RpcException : Exception
    {
        public readonly RpcError OriginalError;
        public int Code => OriginalError.Code;

        internal RpcException(RpcError error) : base(error.Message)
        {
            OriginalError = error;
        }
    }

    struct RawUnsubscribeResponse
    {

    }

    struct RawUnsubscribeRequest
    {

    }

    class Payload
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc;
        [JsonProperty("method")]
        public string Method;
        [JsonProperty("id")]
        public object Id;
        [JsonProperty("params")]
        public object Params;
    }

    class Client : IDisposable
    {
        private int _currentId = 0;

        private IObservable<string> _input;
        private IObserver<string> _output;

        public Client(IObservable<string> input, IObserver<string> output)
        {
            // TODO: make sure JSON is in \u encoding from the IPC
            _input = input.Do(x =>
            {
                //Console.WriteLine($"RPC[<-]: '{x}'");
            });
            _output = output;
        }

        private string GeneratePayload(int id, string method, object parameters)
        {
            return JsonConvert.SerializeObject(new Payload
            {
                JsonRpc = "2.0",
                Id = id,
                Method = method,
                Params = parameters
            });
        }

        public IObservable<TResponse> Send<TResponse>(Request request)
        {
            Interlocked.Increment(ref _currentId);
            var id = _currentId;
            var payload = GeneratePayload(id, request.Method, request.Params);

            return _input.DoOnSubscribe(() =>
                {
                    Console.WriteLine($"RPC[->]: {payload}");
                    _output.OnNext(payload);
                })
                .Where(data => data.Length > 0)
                .Select(data =>
                {
                    Console.WriteLine($"RPC[Raw input] '{data}'");
                    try
                    {
                        return JsonConvert.DeserializeObject<RawResponse<TResponse>>(data);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        return null;
                    }
                })
                .Where(response =>
                {
                    Console.WriteLine($"{response.Id} {id} {response.Id.Equals(id)}");
                    return response.Id.Equals(id);
                })
                .Select(response =>
                {
                    if (response.Error != null)
                    {
                        return Observable.Throw<TResponse>(new RpcException(response.Error));
                    }

                    return Observable.Return(response.Result);
                })
                .Switch()
                .Take(1)
                .Do(x => Console.WriteLine($"RPC[Res] {x}"));
        }

        public IObservable<TResponse> Send<TResponse>(SubscriptionRequest<TResponse> subRequest)
        {
            var initRequest = Send<object>(new Request
            {
                Method = subRequest.Method,
                Params = subRequest.Params
            });

            var observable = Observable.CombineLatest(initRequest, _input, (subId, data) =>
            {
                return new Tuple<object, string>(subId, data);
            });

            object subscriptionId = null;

            return observable.Select((tuple) =>
            {
                subscriptionId = tuple.Item1;

                return JsonConvert.DeserializeObject<RawCallback<TResponse>>(tuple.Item2);
                // TODO: other subscription situations
            })
            .Where(callback => callback.Subscription == subscriptionId)
            .Select(callback => callback.Result)
            .TakeWhile(subRequest.CompletionFilter)
            .Do((_) => { }, () =>
            {
                // TODO: send notification to unsub
            });
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _output.OnCompleted();
                    _input = null;
                    _output = null;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
