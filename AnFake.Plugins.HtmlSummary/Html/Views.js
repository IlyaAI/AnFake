/**
 * Execution summary view. 
 *
 * @param node
 * @param ps
 */
function VExecutionSummary(node, ps) {

    // ======  Constructor =========
    var self = this;
    var impl = {
        node: node
    };

    node.html(
        "<div class='targets-list'>" +            
            "<div class='content empty' style='position: relative; width: 100%;'></div>" +
        "</div>");

    // ======  Public interface =========

    /**
     * @public
     * Binds presentation to the view.
     *
     * Expected object "ExecutionSummary"
     *
     * @param ps
     */
    this.bind = function(ps) {        
        var cnt = $(".content", impl.node);
        cnt.empty();
        
        if (ps.Targets.length > 0) {
            cnt.removeClass("empty");
        } else {
            cnt.addClass("empty");
            return;
        }
        
        var div;
        $.each(ps.Targets, function(i, targetPs){
            div = $(
                "<div class='target-summary'>" +
                    "<p class='display-name' data-ps='Name'/>" +                    
                "</div>");

            //gVM.bind(div.find("*[data-ps]"), targetPs);

            //div.find("p.planed-time")
            //    .text(moment(partyPs.party.planedTime).format($.i18n._("common_format_date_short")));
            
            cnt.append(div);
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
 * Target summary view. 
 *
 * @param node
 * @param ps
 */
function VTargetSummary(node, ps) {

    // ======  Constructor =========
    var self = this;
    var impl = {
        node: node
    };

    node.html(
        "<div class='target-summary'>" +
            "<p class='display-name'/>" +
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
        impl.node.find("p.display-name")
            .text(ps.Name);
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
