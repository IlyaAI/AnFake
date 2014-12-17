/**
 * Build summary view. 
 *
 * @param node
 * @param ps
 */
function VBuildSummary(node, ps) {

    // ======  Constructor =========
    var self = this;
    var impl = {
        node: node
    };

    node.html(
        "<div class='build-summary'>" +
            "<p class='level0'>" +
                "<span class='status-icon'></span>&nbsp;" +
                "<span>Build&nbsp;</span>" +
                "<span></span>" +
            "</p>" +
            "<div class='targets-overview'></div>" +
            "<div class='targets-details'></div>" +
        "</div>");

    // ======  Public interface =========

    /**
     * @public
     * Binds presentation to the view.
     *
     * Expected object "BuildSummary"
     *
     * @param ps
     */
    this.bind = function (ps) {
        impl.node.find(".status-icon")
            .addClass(getIconOfBuildStatus(ps.Status));
        impl.node.find("p > span:eq(2)")
            .text(getHumanReadableBuildStatus(ps.Status));

        if (ps.Targets.length == 0)
            return;

        var div;

        if (ps.Targets.length > 1) {
            var overviews = impl.node.find(".targets-overview");
            $.each(ps.Targets, function (i, targetPs) {
                div = $("<div class='level1' data-v='VTargetOverview'></div");
                overviews.append(div);
                gVM.bind(div, targetPs);
            });
        }

        var details = impl.node.find(".targets-details");
        $.each(ps.Targets, function (i, targetPs) {
            div = $("<div class='level1' data-v='VTargetDetails'></div");
            details.append(div);
            gVM.bind(div, targetPs);            
        });
    };
    if (ps) {
        this.bind(ps);
    }

    /**
     * @public
     * Cleanup internal data.
     */
    this.dispose = function() {
    };    
}

/**
 * Target overview view. 
 *
 * @param node
 * @param ps
 */
function VTargetOverview(node, ps) {

    // ======  Constructor =========
    var self = this;
    var impl = {
        node: node
    };

    node.html(
        "<div class='target-overview'>" +            
            "<span class='state-icon'></span>" +
            "<span class='target-name'></span>&nbsp;" +
            "<span class='level-error' style='display: none'></span>" +
            "<span class='level-warning' style='display: none'></span>" +            
        "</div>");

    // ======  Public interface =========

    /**
     * @public
     * Binds presentation to the view.
     *
     * Expected object "TargetSummary"
     *
     * @param ps
     */
    this.bind = function (ps) {
        impl.node.find(".state-icon")
            .addClass(getIconOfTargetState(ps.State));

        impl.node.find(".target-name")
            .text(ps.Name);

        if (ps.ErrorsCount > 0) {
            impl.node.find("span.level-error")
                .text(ps.ErrorsCount + " error(s) ")
                .show();
        }

        if (ps.WarningsCount > 0) {
            impl.node.find("span.level-warning")
                .text(ps.WarningsCount + " warning(s)")
                .show();
        }
    };
    if (ps) {
        this.bind(ps);
    }

    /**
     * @public
     * Cleanup internal data.
     */
    this.dispose = function () {
    };
}

/**
 * Target details view. 
 *
 * @param node
 * @param ps
 */
function VTargetDetails(node, ps) {

    // ======  Constructor =========
    var self = this;
    var impl = {
        node: node
    };

    node.html(
        "<div class='target-details'>" +
            "<div class='title' style='position: relative'>" +
                "<div class='separator'></div>" +
                "<span>" +
                    "<span class='state-icon'></span>" +
                    "<span class='target-name'></span>" +
                "</span>" +
            "</div>" +
            "<div class='children'></div>" +
            "<div class='content'>" +
                "<div class='messages'></div>" +
                "<div class='overall'>" +                
                    "<span class='level-error'></span>" +
                    "<span class='level-warning'></span>" +
                    "<span class='level-summary'></span>" +
                "</div>" +
            "</div>" +
        "</div>");

    // ======  Public interface =========

    /**
     * @public
     * Binds presentation to the view.
     *
     * Expected object "TargetSummary"
     *
     * @param ps
     */
    this.bind = function (ps) {
        impl.node.find(".state-icon")
            .addClass(getIconOfTargetState(ps.State));

        impl.node.find(".target-name")
            .text(ps.Name);        

        var div;

        if (ps.Children.length > 1 || (ps.Children.length == 1 && ps.Children[0].Name != ps.Name)) {
            var children = impl.node.find(".children");
            $.each(ps.Children, function(i, targetPs) {
                div = $("<div class='level2' data-v='VTargetDetails'></div");
                children.append(div);
                gVM.bind(div, targetPs);
            });
        } else {
            impl.node.find("span.level-error")
                .text(ps.ErrorsCount + " error(s) ");

            impl.node.find("span.level-warning")
                .text(ps.WarningsCount + " warning(s) ");

            impl.node.find("span.level-summary")
                .text(ps.MessagesCount + " messages(s)");

            var messages = impl.node.find(".messages");
            $.each(ps.Messages, function (i, msgPs) {
                div = $("<div data-v='VTraceMessage'></div");
                messages.append(div);
                gVM.bind(div, msgPs);
            });
        }        
    };
    if (ps) {
        this.bind(ps);
    }

    /**
     * @public
     * Cleanup internal data.
     */
    this.dispose = function () {
    };
}

/**
 * Trace message view. 
 *
 * @param node
 * @param ps
 */
function VTraceMessage(node, ps) {

    // ======  Constructor =========
    var self = this;
    var impl = {
        node: node
    };

    node.html(
        "<div class='trace-message'>" +
            "<p>" +
                "<span class='level-icon'></span>" +
                "<span class='message'></span>" +
            "</p>" +            
            "<p class='details' style='display: none'></p>" +
        "</div>");

    // ======  Public interface =========

    /**
     * @public
     * Binds presentation to the view.
     *
     * Expected object "TraceMessage"
     *
     * @param ps
     */
    this.bind = function (ps) {
        var level = getIconOfTraceLevel(ps.Level);

        impl.node.find(".level-icon")
            .addClass(level);

        impl.node.find(".message")
            .addClass("level-" + level)
            .text(ps.Message);

        if (ps.Details) {
            impl.node.find(".details")
                .text(ps.Details)
                .show();
        }
    };
    if (ps) {
        this.bind(ps);
    }

    /**
     * @public
     * Cleanup internal data.
     */
    this.dispose = function () {
    };
}

/* Utilities */

function getHumanReadableBuildStatus(status) {
    switch (status) {
        case 0:
            return "In Progress";
        case 1:
            return "Succeeded";
        case 2:
            return "Partially Succeeded";
        case 3:
            return "Failed";
        default:
            return "status Unknown";
    }
}

function getIconOfBuildStatus(status) {
    switch (status) {
        case 0:
            return "in-progress";
        case 1:
            return "succeeded";
        case 2:
            return "partially-succeeded";
        case 3:
            return "failed";
        default:
            return "unknown";
    }
}

function getHumanReadableTargetState(state) {
    switch (state) {
        case 4:
            return "Succeeded";
        case 5:
            return "Partially Succeeded";
        case 6:
            return "Failed";
        default:
            return "state Unknown";
    }
}

function getIconOfTargetState(state) {
    switch (state) {
        case 4:
            return "succeeded";
        case 5:
            return "partially-succeeded";
        case 6:
            return "failed";
        default:
            return "unknown";
    }
}

function getIconOfTraceLevel(level) {
    switch (level) {
        case 0:
            return "debug";
        case 1:
            return "info";
        case 2:
            return "summary";
        case 3:
            return "warning";
        case 4:
            return "error";
        default:
            return "unknown";
    }
}