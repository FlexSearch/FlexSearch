module Helpers
open FlexSearch.Factories
open FlexSearch.Core.Interface
let pluginContainer = PluginContainer(false).Value
let factoryCollection = new FactoryCollection(pluginContainer) :> IFactoryCollection
