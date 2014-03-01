module Helpers
open FlexSearch.Core
let pluginContainer = PluginContainer(false).Value
let factoryCollection = new FactoryCollection(pluginContainer) :> IFactoryCollection
