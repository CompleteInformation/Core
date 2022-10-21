import { Record, Union } from "../base/frontend/web/fable_modules/fable-library.3.7.20/Types.js";
import { record_type, union_type, string_type } from "../base/frontend/web/fable_modules/fable-library.3.7.20/Reflection.js";

export class PluginId extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["PluginId"];
    }
}

export function PluginId$reflection() {
    return union_type("CompleteInformation.Core.PluginId", [], PluginId, () => [[["Item", string_type]]]);
}

export function PluginIdModule_create(value) {
    return new PluginId(0, value);
}

export function PluginIdModule_unwrap(_arg) {
    return _arg.fields[0];
}

export class PluginMetadata extends Record {
    constructor(id, name) {
        super();
        this.id = id;
        this.name = name;
    }
}

export function PluginMetadata$reflection() {
    return record_type("CompleteInformation.Core.PluginMetadata", [], PluginMetadata, () => [["id", PluginId$reflection()], ["name", string_type]]);
}

export function PluginMetadataModule_create(id, name) {
    return new PluginMetadata(PluginIdModule_create(id), name);
}

