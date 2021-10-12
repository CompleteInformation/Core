import { Record, Union } from "../../fable_modules/fable-library.3.4.2/Types.js";
import { record_type, string_type, union_type, uint32_type } from "../../fable_modules/fable-library.3.4.2/Reflection.js";

export class UserId extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["UserId"];
    }
}

export function UserId$reflection() {
    return union_type("CompleteInformation.Core.Api.UserId", [], UserId, () => [[["Item", uint32_type]]]);
}

export class User extends Record {
    constructor(id, name) {
        super();
        this.id = id;
        this.name = name;
    }
}

export function User$reflection() {
    return record_type("CompleteInformation.Core.Api.User", [], User, () => [["id", UserId$reflection()], ["name", string_type]]);
}

//# sourceMappingURL=User.js.map
