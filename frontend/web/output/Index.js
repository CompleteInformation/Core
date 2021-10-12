import { Union, Record } from "./fable_modules/fable-library.3.4.2/Types.js";
import { union_type, record_type, string_type } from "./fable_modules/fable-library.3.4.2/Reflection.js";
import { UserId, UserId$reflection } from "./api/src/User.js";
import { RemotingModule_createApi, RemotingModule_withBaseUrl, Remoting_buildProxy_Z15584635 } from "./fable_modules/Fable.Remoting.Client.7.16.0/Remoting.fs.js";
import { IUserApi$reflection } from "./api/src/Api.js";
import { Cmd_OfAsync_start, Cmd_OfAsyncWith_perform, Cmd_none } from "./fable_modules/Fable.Elmish.3.1.0/cmd.fs.js";
import { Interop_reactApi } from "./fable_modules/Feliz.1.53.0/Interop.fs.js";
import { createElement } from "react";
import { singleton, ofArray } from "./fable_modules/fable-library.3.4.2/List.js";
import { createObj } from "./fable_modules/fable-library.3.4.2/Util.js";
import { Helpers_combineClasses } from "./fable_modules/Feliz.Bulma.2.18.0/ElementBuilders.fs.js";

export class Model extends Record {
    constructor(text) {
        super();
        this.text = text;
    }
}

export function Model$reflection() {
    return record_type("CompleteInformation.Core.Frontend.Web.Index.Model", [], Model, () => [["text", string_type]]);
}

export class Msg extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["GetUser", "SetText"];
    }
}

export function Msg$reflection() {
    return union_type("CompleteInformation.Core.Frontend.Web.Index.Msg", [], Msg, () => [[["Item", UserId$reflection()]], [["Item", string_type]]]);
}

export const todosApi = Remoting_buildProxy_Z15584635(RemotingModule_withBaseUrl("http://localhost:8081", RemotingModule_createApi()), {
    ResolveType: IUserApi$reflection,
});

export function init() {
    const model = new Model("start");
    const cmd = Cmd_none();
    return [model, cmd];
}

export function update(msg, model) {
    if (msg.tag === 1) {
        const text = msg.fields[0];
        return [new Model(text), Cmd_none()];
    }
    else {
        const userId = msg.fields[0];
        const cmd = Cmd_OfAsyncWith_perform((x) => {
            Cmd_OfAsync_start(x);
        }, todosApi.get, userId, (arg) => {
            let _arg1, user;
            return new Msg(1, (_arg1 = arg, (_arg1 == null) ? "error" : ((user = _arg1, user.name))));
        });
        return [model, cmd];
    }
}

export const navBrand = (() => {
    let props_1;
    const elms = singleton((props_1 = ofArray([["href", "https://safe-stack.github.io/"], ["className", "is-active"], ["children", Interop_reactApi.Children.toArray([createElement("img", {
        src: "/favicon.png",
        alt: "Logo",
    })])]]), createElement("a", createObj(Helpers_combineClasses("navbar-item", props_1)))));
    return createElement("div", {
        className: "navbar-brand",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    });
})();

export function containerBox(model, dispatch) {
    let props_4;
    const elms = singleton((props_4 = ofArray([["className", "is-grouped"], ["children", Interop_reactApi.Children.toArray([createElement("p", createObj(Helpers_combineClasses("", singleton(["children", model.text])))), createElement("button", createObj(Helpers_combineClasses("button", ofArray([["children", "Fetch"], ["onClick", (_arg1) => {
        dispatch(new Msg(0, new UserId(0, 1)));
    }]]))))])]]), createElement("div", createObj(Helpers_combineClasses("field", props_4)))));
    return createElement("div", {
        className: "box",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    });
}

export function view(model, dispatch) {
    let elms_2, elms_1, elms_4, elms_3, props_5;
    const props_9 = ofArray([["className", "is-fullheight"], ["className", "is-primary"], ["style", {
        backgroundSize: "cover",
        backgroundImage: "url(\u0027https://unsplash.it/1200/900?random\u0027)",
        backgroundPosition: "no-repeat center center fixed",
    }], ["children", Interop_reactApi.Children.toArray([(elms_2 = singleton((elms_1 = singleton(createElement("div", {
        className: "container",
        children: Interop_reactApi.Children.toArray([navBrand]),
    })), createElement("nav", {
        className: "navbar",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    }))), createElement("div", {
        className: "hero-head",
        children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
    })), (elms_4 = singleton((elms_3 = singleton((props_5 = ofArray([["className", "is-6"], ["className", "is-offset-3"], ["children", Interop_reactApi.Children.toArray([createElement("h1", createObj(Helpers_combineClasses("title", ofArray([["className", "has-text-centered"], ["children", "SAFE.App"]])))), containerBox(model, dispatch)])]]), createElement("div", createObj(Helpers_combineClasses("column", props_5))))), createElement("div", {
        className: "container",
        children: Interop_reactApi.Children.toArray(Array.from(elms_3)),
    }))), createElement("div", {
        className: "hero-body",
        children: Interop_reactApi.Children.toArray(Array.from(elms_4)),
    }))])]]);
    return createElement("section", createObj(Helpers_combineClasses("hero", props_9)));
}

//# sourceMappingURL=Index.js.map
