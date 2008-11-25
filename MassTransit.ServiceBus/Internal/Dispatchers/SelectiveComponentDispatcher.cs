// Copyright 2007-2008 The Apache Software Foundation.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.Internal
{
    public class SelectiveComponentDispatcher<TComponent, TMessage> :
        ComponentDispatcherBase<TComponent>,
        Consumes<TMessage>.Selected
        where TMessage : class
        where TComponent : class, Consumes<TMessage>.Selected
    {
        public SelectiveComponentDispatcher(IDispatcherContext context)
            : base(context)
        {
        }

        public bool Accept(TMessage message)
        {
            TComponent component = BuildComponent();

            try
            {
                return component.Accept(message);
            }
            finally
            {
                _context.Builder.Release(component);
            }
        }

        public void Consume(TMessage message)
        {
            TComponent component = BuildComponent();

            try
            {
                component.Consume(message);
            }
            finally
            {
                _context.Builder.Release(component);
            }
        }
    }
}