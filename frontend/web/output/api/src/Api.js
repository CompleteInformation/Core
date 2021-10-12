import { Record } from "../../fable_modules/fable-library.3.4.2/Types.js";
import { User$reflection, UserId$reflection } from "./User.js";
import { record_type, lambda_type, class_type, option_type } from "../../fable_modules/fable-library.3.4.2/Reflection.js";

export class IUserApi extends Record {
    constructor(get$) {
        super();
        this.get = get$;
    }
}

export function IUserApi$reflection() {
    return record_type("CompleteInformation.Core.Api.IUserApi", [], IUserApi, () => [["get", lambda_type(UserId$reflection(), class_type("Microsoft.FSharp.Control.FSharpAsync`1", [option_type(User$reflection())]))]]);
}

//# sourceMappingURL=Api.js.map
