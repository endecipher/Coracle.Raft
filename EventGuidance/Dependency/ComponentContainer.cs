using System;

namespace EventGuidance.Dependency
{
    /// <summary>
    /// Service Locator 
    /// </summary>
    [Obsolete]
    public class ComponentContainer
    {
        private static bool IsRegistered { get; set; } = false;

        public static IDependencyContainer _instance = null;

        public static IDependencyContainer Instance { 
            get 
            { 
                return _instance; 
            } 
            set 
            {
                var ContainerAlreadyRegistered = IsRegistered;

                if (!ContainerAlreadyRegistered)
                {
                    _instance = value;
                    IsRegistered = true;
                }
                else
                {
                    throw new InvalidOperationException(nameof(ContainerAlreadyRegistered));
                }
            } 
        }
    }
}
