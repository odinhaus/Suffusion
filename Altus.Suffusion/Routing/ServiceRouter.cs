﻿using Altus.Suffusion.Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Altus.Suffusion.Routing
{
    public delegate void TaskRoute<T>(T payload);
    public delegate U TaskRoute<T, U>(T payload);
    public class ServiceRouter : IServiceRouter
    {
        Dictionary<string, ServiceRoute> _routes = new Dictionary<string, ServiceRoute>();

        public ServiceRoute GetRoute(string channelId, Type requestType)
        {
            ServiceRoute route;
            if (requestType == typeof(NoArgs)) requestType = null;
            var key = channelId + (requestType?.FullName ?? "null");
            lock (_routes)
            {
                if (!_routes.TryGetValue(key, out route))
                {
                    return null;
                }
            }
            return route;
        }

        public ServiceRoute GetRoute<TRequest>(string channelId)
        {
            return GetRoute(channelId, typeof(TRequest));
        }

        public ServiceRoute<TRequest, TResult> Route<THandler, TRequest, TResult>(string channelId, Expression<Func<THandler, TRequest, TResult>> handler)
        {
            var route = new ServiceRoute<TRequest, TResult>() { Handler = CreateDelegate(handler), HasParameters = true };
            var key = channelId + typeof(TRequest).FullName;
            lock (_routes)
            {
                if (!_routes.ContainsKey(key))
                {
                    _routes.Add(key, route);
                    App.Resolve<IChannelService>().Create(channelId); // gets the channel up and running
                }
                else
                {
                    _routes[key] = route;
                }
            }
            return route;
        }

        public ServiceRoute<NoArgs, TResult> Route<THandler, TResult>(string channelId, Expression<Func<THandler, TResult>> handler)
        {
            var route = new ServiceRoute<NoArgs, TResult>() { Handler = CreateDelegate(handler), HasParameters = false };
            var key = channelId + "null";
            lock (_routes)
            {
                if (!_routes.ContainsKey(key))
                {
                    _routes.Add(key, route);
                    App.Resolve<IChannelService>().Create(channelId); // gets the channel up and running
                }
                else
                {
                    _routes[key] = route;
                }
            }
            return route;
        }

        public ServiceRoute<TMessage, NoReturn> Route<THandler, TMessage>(string channelId, Expression<Action<THandler, TMessage>> handler)
        {
            var route = new ServiceRoute<TMessage, NoReturn>() { Handler = CreateDelegate(handler), HasParameters = true };
            var key = channelId + typeof(TMessage).FullName;
            lock (_routes)
            {
                if (!_routes.ContainsKey(key))
                {
                    _routes.Add(key, route);
                    App.Resolve<IChannelService>().Create(channelId); // gets the channel up and running
                }
                else
                {
                    _routes[key] = route;
                }
            }
            return route;
        }
        public ServiceRoute<NoArgs, NoReturn> Route<THandler>(string channelId, Expression<Action<THandler>> handler)
        {
            var route = new ServiceRoute<NoArgs, NoReturn>() { Handler = CreateDelegate(handler), HasParameters = true };
            var key = channelId + typeof(NoArgs).FullName;
            lock (_routes)
            {
                if (!_routes.ContainsKey(key))
                {
                    _routes.Add(key, route);
                    App.Resolve<IChannelService>().Create(channelId); // gets the channel up and running
                }
                else
                {
                    _routes[key] = route;
                }
            }
            return route;
        }



        private Func<TPayload, TResult> CreateDelegate<THandler, TPayload, TResult>(Expression<Func<THandler, TPayload, TResult>> handler)
        {
            var bodyCall = (MethodCallExpression)((LambdaExpression)handler).Body;
            var handlerType = bodyCall.Object.Type;
            var createInstanceCall = typeof(App).GetMethod("Resolve").MakeGenericMethod(handlerType);
            // creates new target instance from DI Container
            var instance = Expression.Call(null, createInstanceCall);

            var newBodyCall = Expression.Call(instance, bodyCall.Method, bodyCall.Arguments);
            var lambda = Expression.Lambda<Func<TPayload, TResult>>(newBodyCall, bodyCall.Arguments.OfType<ParameterExpression>());
            return lambda.Compile();
        }


        private Func<TResult> CreateDelegate<THandler, TResult>(Expression<Func<THandler, TResult>> handler)
        {
            var bodyCall = (MethodCallExpression)((LambdaExpression)handler).Body;
            var handlerType = bodyCall.Object.Type;
            var createInstanceCall = typeof(App).GetMethod("Resolve").MakeGenericMethod(handlerType);
            // creates new target instance from DI Container
            var instance = Expression.Call(null, createInstanceCall);

            var newBodyCall = Expression.Call(instance, bodyCall.Method, bodyCall.Arguments);
            var lambda = Expression.Lambda<Func<TResult>>(newBodyCall, bodyCall.Arguments.OfType<ParameterExpression>());
            return lambda.Compile();
        }

        private Action<TMessage> CreateDelegate<THandler, TMessage>(Expression<Action<THandler, TMessage>> handler)
        {
            var bodyCall = (MethodCallExpression)((LambdaExpression)handler).Body;
            var handlerType = bodyCall.Object.Type;
            var createInstanceCall = typeof(App).GetMethod("Resolve").MakeGenericMethod(handlerType);
            // creates new target instance from DI Container
            var instance = Expression.Call(null, createInstanceCall);

            var newBodyCall = Expression.Call(instance, bodyCall.Method, bodyCall.Arguments);
            var lambda = Expression.Lambda<Action<TMessage>>(newBodyCall, bodyCall.Arguments.OfType<ParameterExpression>());
            return lambda.Compile();
        }

        private System.Action CreateDelegate<THandler>(Expression<Action<THandler>> handler)
        {
            var bodyCall = (MethodCallExpression)((LambdaExpression)handler).Body;
            var handlerType = bodyCall.Object.Type;
            var createInstanceCall = typeof(App).GetMethod("Resolve").MakeGenericMethod(handlerType);
            // creates new target instance from DI Container
            var instance = Expression.Call(null, createInstanceCall);

            var newBodyCall = Expression.Call(instance, bodyCall.Method, bodyCall.Arguments);
            var lambda = Expression.Lambda<System.Action>(newBodyCall, bodyCall.Arguments.OfType<ParameterExpression>());
            return lambda.Compile();
        }

    }
}
