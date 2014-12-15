/**
 * Static view manager.
 */
function ViewManager() {
    var self = this;
    var impl = {
        VID: "__view",
        mappings: {
        }
    };

    impl.getViewType = function(node, ps, resolver) {
        var viewType;

        if (resolver && ps) {
            viewType = resolver(node, ps);
            if (viewType)
                return viewType;
        }

        viewType = node.data("v");
        if (viewType)
            return window[viewType];

        if (!ps)
            return undefined;

        return impl.mappings.hasOwnProperty(ps.__type)
            ? impl.mappings[ps.__type]
            : undefined;
    };

    impl.createView = function(viewType, node, ps) {
        if (!viewType)
            return null;

        var view = new viewType(node, ps);
        self.localize(node);

        return view;
    };

    impl.bindView = function(node, ps, resolver) {
        var viewType = impl.getViewType(node, ps, resolver);

        var view = node.data(impl.VID);
        if (view) {
            if (viewType && !(view instanceof viewType)) {
                view.dispose();

                node.removeData(impl.VID);
                node.empty();

                view = impl.createView(viewType, node, ps);
                if (view) {
                    node.data(impl.VID, view);
                }
            } else if (ps) {
                view.bind(ps);
            }
        } else {
            view = impl.createView(viewType, node, ps);
            if (view) {
                node.data(impl.VID, view);
            }
        }

        return view;
    };

    impl.unbindView = function(node) {
        var view = node.data(impl.VID);
        if (view) {
            view.dispose();
            node.removeData(impl.VID);
        }
    };

    /**
     * @public
     * Returns associated view for first node of node selector.
     *
     * @param node - jquery node selector
     */
    this.getView = function(node) {
        return node.data(impl.VID);
    };

    /**
     * @public
     * Scans node and its children and localizes found HTML elements.
     *
     * @param node
     */
    this.localize = function (node) {
        if (typeof $._t == "undefined")
            return;

        $("*[data-i18n]", node).each(function() {
            var e = $(this);
            e._t(e.data("i18n"));
        });
        $("input[placeholder|='i18n'],textarea[placeholder|='i18n']", node).each(function() {
            var e = $(this);
            e.attr("placeholder", $.i18n._(e.attr("placeholder").substr(5)));
        });
        $("input[title|='i18n'],li[title|='i18n']", node).each(function() {
            var e = $(this);
            e.attr("title", $.i18n._(e.attr("title").substr(5)));
        });
    };

    /**
     * @public
     * Creates views for specified node selector and binds it to presentation.
     *
     * The view type determined as:
     *  a. type returned by resolver
     *  b. value of data-v attribute of node
     *  c. mapping of ps.__type to registered view type
     *
     * Resolver should have a following signature:
     * function resolve(node, ps) {
     *   return MyViewType or null or undefined;
     * }
     *
     * The presentation to be bound determined as:
     *  a. ps[name] where name is value of data-ps attribute of node
     *  b. ps itself if data-ps is undefined
     *
     * @param node - jquery node selector; required
     * @param ps - presentation to be bound; optional
     * @param resolver - view type resolver function; optional
     * @return array of views
     */
    this.bind = function(node, ps, resolver) {
        var views = [];

        node.each(function(index) {
            var subNode = $(this);
            var subPsName = subNode.data("ps");

            var subPs = null;
            if (!subPsName) {
                subPs = ps;
            } else if (ps && ps.hasOwnProperty(subPsName)) {
                subPs = ps[subPsName];
            }

            var v = impl.bindView(subNode, subPs, resolver);
            if (v) {
                views.push(v);
            }
        });

        return views;
    };

    /**
     * @public
     * Unbinds and disposes views for specified node selector
     * then removes any child nodes of each node from selector.
     *
     * Unbind should be called on top level DOM node, view's dispose()
     * method should NOT call unbind() to prevent multi-step step DOM removing.
     *
     * @param node - jquery node selector; required
     */
    this.unbind = function(node) {
        node.each(function(index) {
            impl.unbindView($(this));
        });

        node.empty();
    };

    /**
     * @public
     * Disposes views for specified node selector but doesn't affect DOM.
     *
     * View's dispose() method should call release() to dispose child views.
     *
     * @param node - jquery node selector; required
     */
    this.release = function(node) {
        node.each(function(index) {
            impl.unbindView($(this));
        });
    };

    /**
     * @public
     * Registers mapping of presentation type to view type.
     *
     * @param psType - presentation type as string
     * @param vType - view type as class
     */
    this.registerView = function(psType, vType) {
        impl.mappings[psType] = vType;
    };
}

var gVM = new ViewManager();